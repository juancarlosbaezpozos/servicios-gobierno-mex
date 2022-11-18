using System;
using System.Collections.Generic;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// <auto-generated />
//
//    var afiliationResponse = AfiliationResponse.FromJson(jsonString);

namespace AltergoAPI.Nss.Core.Models
{
    public partial class ListaMilitante
    {
        [JsonProperty("siglas", NullValueHandling = NullValueHandling.Ignore)]
        public string Siglas { get; set; }

        [JsonProperty("nombreEstado", NullValueHandling = NullValueHandling.Ignore)]
        public string NombreEstado { get; set; }

        [JsonProperty("idEstadoPartido", NullValueHandling = NullValueHandling.Ignore)]
        public long? IdEstadoPartido { get; set; }

        [JsonProperty("tipoAsociacion", NullValueHandling = NullValueHandling.Ignore)]
        public long? TipoAsociacion { get; set; }

        [JsonProperty("idPartidoPolitico", NullValueHandling = NullValueHandling.Ignore)]
        public long? IdPartidoPolitico { get; set; }

        [JsonProperty("idMilitante")]
        public object IdMilitante { get; set; }

        [JsonProperty("nombre", NullValueHandling = NullValueHandling.Ignore)]
        public string Nombre { get; set; }

        [JsonProperty("apellidoPaterno", NullValueHandling = NullValueHandling.Ignore)]
        public string ApellidoPaterno { get; set; }

        [JsonProperty("apellidoMaterno", NullValueHandling = NullValueHandling.Ignore)]
        public string ApellidoMaterno { get; set; }

        [JsonProperty("nombreCompleto")]
        public object NombreCompleto { get; set; }

        [JsonProperty("fechaAfiliacion", NullValueHandling = NullValueHandling.Ignore)]
        public string FechaAfiliacion { get; set; }

        [JsonProperty("emblema", NullValueHandling = NullValueHandling.Ignore)]
        public string Emblema { get; set; }

        [JsonProperty("nombrePartido", NullValueHandling = NullValueHandling.Ignore)]
        public string NombrePartido { get; set; }

        [JsonProperty("cancelaciones", NullValueHandling = NullValueHandling.Ignore)]
        public AfiliationResponse Cancelaciones { get; set; }
    }

    public partial class AfiliationResponse
    {
        [JsonProperty("wsrest", NullValueHandling = NullValueHandling.Ignore)]
        public Wsrest Wsrest { get; set; }

        [JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
        public int Code { get; set; }

        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; set; }

        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        [JsonProperty("causa", NullValueHandling = NullValueHandling.Ignore)]
        public string Causa { get; set; }

        [JsonProperty("listaMilitantes", NullValueHandling = NullValueHandling.Ignore)]
        public List<ListaMilitante> ListaMilitantes { get; set; }

        [JsonProperty("listaCancelados", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> ListaCancelados { get; set; }
    }

    public partial class Wsrest
    {
        [JsonProperty("fecha", NullValueHandling = NullValueHandling.Ignore)]
        public string Fecha { get; set; }

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; }

        [JsonProperty("descripcion", NullValueHandling = NullValueHandling.Ignore)]
        public string Descripcion { get; set; }

        [JsonProperty("actualizacion", NullValueHandling = NullValueHandling.Ignore)]
        public string Actualizacion { get; set; }
    }

    public partial class AfiliationResponse
    {
        public static AfiliationResponse FromJson(string json) => JsonConvert.DeserializeObject<AfiliationResponse>(json, Models.ConverterAfiliationResponse.Settings);
    }

    public static class SerializeAfiliationResponse
    {
        public static string ToJson(this AfiliationResponse self) => JsonConvert.SerializeObject(self, Models.ConverterAfiliationResponse.Settings);
    }

    internal static class ConverterAfiliationResponse
    {
        public static readonly JsonSerializerSettings Settings = new()
{MetadataPropertyHandling = MetadataPropertyHandling.Ignore, DateParseHandling = DateParseHandling.None, Converters = {new IsoDateTimeConverter{DateTimeStyles = DateTimeStyles.AssumeUniversal}}, };
    }
}
