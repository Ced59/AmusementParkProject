using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;
using Common.General;
using System;
using System.Collections.Generic;

namespace Entities.Model.Searching
{
    /// <summary>
    /// Document utilisé pour la recherche unifiée. 
    /// Hérite de GeolocatedEntity pour pouvoir faire des requêtes géospatiales si nécessaire.
    /// </summary>
    public class SearchItem : GeolocatedEntity
    {
        /// <summary>
        /// “park_{parkId}” ou “coaster_{coasterId}”, etc.
        /// Permet de retrouver facilement l’élément d’origine pour un clic sur le résultat.
        /// </summary>
        [BsonElement("originalId")]
        public string OriginalId { get; set; } = string.Empty;

        /// <summary>
        /// “park”, “coaster”, “restaurant”, “hotel”, etc.
        /// </summary>
        [BsonElement("category")]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Champ principal pour la recherche texte (le nom de l’entité).
        /// </summary>
        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Si vous voulez lister aussi une description plus longue
        /// (par exemple le pays + type + phrase courte), créez-le ici :
        /// </summary>
        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Mots-clés additionnels pour la recherche (ex. nom du pays, tags…).
        /// </summary>
        [BsonElement("keywords")]
        public IEnumerable<string> Keywords { get; set; } = new List<string>();

        /// <summary>
        /// Score composite, si vous voulez mélanger textScore + popularité + date, etc.
        /// </summary>
        [BsonElement("compositeScore")]
        [BsonRepresentation(BsonType.Double)]
        public double CompositeScore { get; set; } = 0.0;
    }
}