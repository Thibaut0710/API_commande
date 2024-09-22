using System.ComponentModel.DataAnnotations;

namespace API_Commande.Models
{
    public class Commande
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom du client est obligatoire.")]
        [StringLength(100, ErrorMessage = "Le nom du client ne peut dépasser 100 caractères.")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "La date de commande est obligatoire.")]
        [DataType(DataType.Date)]
        [CustomValidation(typeof(Commande), nameof(ValidateOrderDate))]
        public DateTime OrderDate { get; set; }

        [Required(ErrorMessage = "Le montant total est obligatoire.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant total doit être supérieur à 0.")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "L'ID du client est obligatoire.")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du client doit être un nombre positif.")]
        public int ClientID { get; set; }

        [Required(ErrorMessage = "Au moins un produit doit être associé à la commande.")]
        [MinLength(1, ErrorMessage = "Au moins un produit doit être associé à la commande.")]
        public List<int> ProduitIDs { get; set; }

        // Méthode de validation personnalisée pour OrderDate
        public static ValidationResult? ValidateOrderDate(DateTime orderDate, ValidationContext context)
        {
            if (orderDate > DateTime.Now)
            {
                return new ValidationResult("La date de commande ne peut pas être dans le futur.");
            }
            return ValidationResult.Success;
        }
    }
}
