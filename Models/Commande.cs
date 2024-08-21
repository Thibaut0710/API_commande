namespace API_Commande.Models
{
    public class Commande
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public int ClientID { get; set; }
        public List<int> ProduitIDs { get; set; }
    }
}
