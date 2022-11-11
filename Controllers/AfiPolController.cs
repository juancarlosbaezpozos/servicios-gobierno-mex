using AltergoAPI.Nss.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestSharp;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AltergoAPI.Nss.Core.Controllers
{
    [SwaggerTag("APIS INE")]
    [Produces("application/json")]
    [Route("Ine")]
    [ApiController]
    public class AfiPolController : ControllerBase
    {
        private const string ApiUrl = "https://servicios.ine.mx/militantesService/rest/service/detalleMilitante";
        private const string Agente = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36";

        /// <summary>
        /// Obtiene la afiliación politica de una persona mediante la clave de elector
        /// </summary>
        /// <param name="claveElector" example="RJPRSN85121707M000">Clave de Elector a 18 caracteres alfanuméricos</param>
        /// <returns>Partido político de pertenencia</returns>
        /// <response code="200">Respuesta Valida</response>
        /// <response code="400">Respuesta Inválida: La clave de elector no tiene el formator correcto</response>
        /// <response code="204">Respuesta Inválida: Sin contenido</response>
        [HttpGet("VerificarAfiliacion")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetRecord([FromQuery, Required] string claveElector)
        {
            const string clvPattern = @"^([A-Z&]|[a-z&]{1})([A-Z&]|[a-z&]{1})([A-Z&]|[a-z&]{1})([A-Z&]|[a-z&]{1})([A-Z&]|[a-z&]{1})([A-Z&]|[a-z&]{1})([0-9]{8})([HM]|[hm]{1})([0-9]{3})$";
            var rgPattern = Regex.Match(claveElector, clvPattern, RegexOptions.IgnoreCase);
            if (!rgPattern.Success)
            {
                const string err = "La clave de elector no tiene el formato correcto";
                return BadRequest(err);
            }

            var restClient = new RestClient(ApiUrl) { Timeout = -1 };
            var request = new RestRequest(Method.POST);
            request.AddHeader("Accept", "application/json");
            var body = $"{{\"clave\": \"{claveElector}\"}}";
            request.AddParameter("text/plain", body, ParameterType.RequestBody);

            restClient.UserAgent = Agente;
            var response = await restClient.ExecuteAsync(request);
            if (response.StatusCode != HttpStatusCode.OK) return NoContent();
            var result = AfiliationResponse.FromJson(response.Content);
            if (result.ListaMilitantes is { Count: > 0 })
            {
                var militancia = result.ListaMilitantes[0];
                object o = new
                {
                    Message = "PERTENECE A UN PARTIDO POLITICO",
                    Militante = $"{militancia.Nombre} {militancia.ApellidoPaterno} {militancia.ApellidoMaterno}".ToUpperInvariant(),
                    Partido = militancia.NombrePartido.ToUpperInvariant(),
                    MiembroDesde = militancia.FechaAfiliacion.ToUpperInvariant()
                };

                return Ok(o);
            }
            else
            {
                object o = new
                {
                    result.Message
                };

                return Ok(o);
            }
        }//Fin

    }
}
