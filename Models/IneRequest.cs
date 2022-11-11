namespace AltergoAPI.Nss.Core.Models
{
    public class IneRequest
    {
        /// <summary>
        /// Modelo de credencial: C, D, E, F, G
        /// </summary>
        public string Modelo { get; set; }
    }

    public class IneCRequest : IneRequest
    {
        public string ClaveElector { get; set; }
        public string Ocr { get; set; }
        public string NumeroEmision { get; set; }
        public new string Modelo => "c";
    }

    public class IneERequest : IneRequest
    {
        public string IdCiudadano { get; set; }
        public string Cic { get; set; }
        public new string Modelo => "e";
    }
}
