namespace Dtos.Searching
{
    public class SearchResultDto
    {
        /// <summary>
        /// Champ principal pour la recherche texte (le nom de l’entité).
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Si vous voulez lister aussi une description plus longue
        /// (par exemple le pays + type + phrase courte), créez-le ici :
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}