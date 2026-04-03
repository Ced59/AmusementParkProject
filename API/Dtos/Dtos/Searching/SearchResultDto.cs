namespace Dtos.Searching
{
    public class SearchResultDto
    {
        /// <summary>
        /// Champ principal pour la recherche texte (le nom de l’entité).
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description synthétique affichée dans les résultats.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Catégorie publique de l’élément recherché.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Identifiant d’origine de l’élément indexé.
        /// </summary>
        public string OriginalId { get; set; } = string.Empty;
    }
}
