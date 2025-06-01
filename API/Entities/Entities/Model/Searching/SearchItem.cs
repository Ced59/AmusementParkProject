using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.General;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Searching
{
    /// <summary>
    /// Document utilisé pour la recherche unifiée.
    /// </summary>
    public class SearchItem : GeolocatedEntity
    {
        /// <summary>
        /// Chemin vers le document d'origine.
        /// </summary>
        [BsonElement("originalId")]
        public string OriginalId { get; set; } = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        [BsonElement("category")]
        public string Category { get; set; } = string.Empty;

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("keywords")]
        public IEnumerable<string> Keywords { get; set; } = new List<string>();

        [BsonElement("compositeScore")]
        public double CompositeScire { get; set; }
    }
}
