import { TestBed } from '@angular/core/testing';
import { Observable, Subject } from 'rxjs';

import { StandaloneAttraction } from '@app/models/standalone-attractions/standalone-attraction';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { SKIP_AUTHORIZATION_HEADER } from '@core/http/auth/auth-request-policy';
import { ManufacturersHttpOptions } from '@data-access/manufacturers/manufacturers-api.service';
import {
  STANDALONE_ATTRACTION_DETAIL_MANUFACTURERS_PORT,
  StandaloneAttractionDetailManufacturersPort
} from './standalone-attraction-detail-data.ports';
import { StandaloneAttractionDetailReferenceFacade } from './standalone-attraction-detail-reference.facade';

describe('StandaloneAttractionDetailReferenceFacade', () => {
  let facade: StandaloneAttractionDetailReferenceFacade;
  let responses: Map<string, Subject<AttractionManufacturer>>;
  let requestedIncludeHidden: boolean | null;
  let requestedOptions: ManufacturersHttpOptions | null;

  beforeEach(() => {
    responses = new Map<string, Subject<AttractionManufacturer>>();
    requestedIncludeHidden = null;
    requestedOptions = null;
    const manufacturersPort: StandaloneAttractionDetailManufacturersPort = {
      getAttractionManufacturerById: (
        id: string,
        includeHidden: boolean = false,
        options: ManufacturersHttpOptions = {}
      ): Observable<AttractionManufacturer> => {
        requestedIncludeHidden = includeHidden;
        requestedOptions = options;
        const response: Subject<AttractionManufacturer> = new Subject<AttractionManufacturer>();
        responses.set(id, response);
        return response.asObservable();
      }
    };

    TestBed.configureTestingModule({
      providers: [
        StandaloneAttractionDetailReferenceFacade,
        {
          provide: STANDALONE_ATTRACTION_DETAIL_MANUFACTURERS_PORT,
          useValue: manufacturersPort
        }
      ]
    });
    facade = TestBed.inject(StandaloneAttractionDetailReferenceFacade);
  });

  it('exposes the resolved manufacturer name', () => {
    facade.loadManufacturer(createAttraction('attraction-1', 'manufacturer-1'));

    responses.get('manufacturer-1')?.next({ name: ' Wiegand ' });

    expect(facade.manufacturerName()).toBe('Wiegand');
    expect(requestedIncludeHidden).toBe(false);
    expect(requestedOptions?.context?.get(SKIP_AUTHORIZATION_HEADER)).toBe(true);
  });

  it('ignores a response for an attraction that is no longer active', () => {
    facade.loadManufacturer(createAttraction('attraction-1', 'manufacturer-1'));
    facade.loadManufacturer(createAttraction('attraction-2', 'manufacturer-2'));

    responses.get('manufacturer-1')?.next({ name: 'Ancien constructeur' });
    responses.get('manufacturer-2')?.next({ name: 'Constructeur actuel' });

    expect(facade.manufacturerName()).toBe('Constructeur actuel');
  });
});

function createAttraction(id: string, manufacturerId: string): StandaloneAttraction {
  return {
    id,
    name: id,
    type: 'Attraction',
    attractionDetails: {
      manufacturerId
    },
    isVisible: true,
    adminReviewStatus: 'Validated'
  };
}
