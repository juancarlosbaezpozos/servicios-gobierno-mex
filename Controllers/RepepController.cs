using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestSharp;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AltergoAPI.Nss.Core.Controllers
{
    [SwaggerTag("APIS PROFECO")]
    [Produces("application/json")]
    [Route("Profeco")]
    [ApiController]
    public class RepepController : ControllerBase
    {
        /// <summary>
        /// Verifica si un número telefónico esta registrado en el REPEP:
        /// Registro Público Para Evitar Publicidad
        /// </summary>
        /// <param name="numeroTelefonico" example="5514559123">Número telefónico a 10 digitos (Fijo o Móvil)</param>
        /// <returns>Si un número puede o no puede recibir publicidad</returns>
        /// <response code="200">Respuesta Valida</response>
        /// <response code="400">Respuesta Inválida: El teléfono no tiene el formato correcto</response>
        /// <response code="204">Respuesta Inválida: Sin contenido</response>
        [HttpGet("VerificarRepep")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetRecord([FromQuery, Required] string numeroTelefonico)
        {
            const string clvPattern = @"([0-9]{10})";
            var rgPattern = Regex.Match(numeroTelefonico, clvPattern, RegexOptions.IgnoreCase);
            if (!rgPattern.Success)
            {
                const string err = "El número de teléfono no tiene el formato correcto.";
                return BadRequest(err);
            }

            var client = new RestClient("https://repep.profeco.gob.mx/Solicitudnumero.jsp")
            {
                Timeout = -1
            };
            var request = new RestRequest(Method.GET);
            request.AddHeader("Upgrade-Insecure-Requests", "1");
            client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36";
            request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            request.AddHeader("sec-ch-ua", "\"Chromium\";v=\"94\", \"Google Chrome\";v=\"94\", \";Not A Brand\";v=\"99\"");
            request.AddHeader("sec-ch-ua-mobile", "?0");
            request.AddHeader("sec-ch-ua-platform", "\"Windows\"");
            var response = await client.ExecuteAsync(request);
            if (response.StatusCode != HttpStatusCode.OK) return NoContent();

            var _cookies = response.Cookies;

            var parser = new HtmlParser();
            var document = parser.ParseDocument(response.Content);
            var dynamicElement = document.QuerySelector("input[id='telefono']");
            var dynamicCode = ((AngleSharp.Html.Dom.IHtmlInputElement)dynamicElement)?.Name;
            if (string.IsNullOrWhiteSpace(dynamicCode)) return NoContent();

            var client2 = new RestClient("https://repep.profeco.gob.mx/miregistro.jsp")
            {
                Timeout = -1
            };
            var request2 = new RestRequest(Method.POST);
            request2.AddHeader("sec-ch-ua", "\"Chromium\";v=\"94\", \"Google Chrome\";v=\"94\", \";Not A Brand\";v=\"99\"");
            request2.AddHeader("sec-ch-ua-mobile", "?0");
            request2.AddHeader("sec-ch-ua-platform", "\"Windows\"");
            request2.AddHeader("Upgrade-Insecure-Requests", "1");
            request2.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            client2.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36";
            request2.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            request2.AddParameter($"{dynamicCode}", $"{numeroTelefonico}");
            foreach (var _cookie in _cookies)
            {
                request2.AddParameter($"{_cookie.Name}", $"{_cookie.Value}", ParameterType.Cookie);
            }
            var response2 = await client2.ExecuteAsync(request2);
            if (response2.StatusCode != HttpStatusCode.OK) return NoContent();

            var document2 = parser.ParseDocument(response2.Content);
            var noRegistrado = $"El número {numeroTelefonico} no está registrado";
            var preRegistrado = document2.All.Where(m => m.TextContent.Contains(noRegistrado));
            var estaRegistrado = !preRegistrado.Any();

            object resultadoFinal;
            if (estaRegistrado)
            {
                var preResult = document2.QuerySelector("div[id='contenido']");
                var newResult = preResult?.QuerySelectorAll("div")
                    .Select(x => new
                    {
                        Data = x.TextContent
                    }).Skip(6).Take(7);

                var llave = "";
                var valor = "";

                var estado = "";
                var fechaRegistro = "";
                var entidad = "";
                var area = "";
                var numTel = "";
                if (newResult != null)
                    foreach (var s in newResult)
                    {
                        var row = s.Data.Replace("\t", "").Trim().Split("\n");
                        foreach (var s1 in row)
                        {
                            if (!string.IsNullOrEmpty(s1.ToUpperInvariant().Trim()))
                            {
                                if (string.IsNullOrEmpty(llave))
                                {
                                    llave = s1.ToUpperInvariant().Trim();
                                }
                                else if (string.IsNullOrEmpty(valor))
                                {
                                    valor = s1.ToUpperInvariant().Trim();
                                }

                                if (!string.IsNullOrEmpty(llave) && !string.IsNullOrEmpty(valor))
                                {
                                    switch (llave)
                                    {
                                        case "ESTATUS:":
                                            estado = valor;
                                            break;
                                        case "FECHA DE REGISTRO:":
                                            fechaRegistro = valor;
                                            break;
                                        case "ENTIDAD:":
                                            entidad = valor;
                                            break;
                                        case "CÓDIGO DE ÁREA:":
                                            area = valor;
                                            break;
                                        case "NÚMERO TELEFÓNICO:":
                                            numTel = valor;
                                            break;
                                    }
                                }
                            }
                        }

                        llave = string.Empty;
                        valor = string.Empty;
                    }

                resultadoFinal = new
                {
                    estado = "REGISTRADO",
                    detalles = new
                    {
                        estatus = estado,
                        fechaRegistro,
                        entidad,
                        codigoArea = area,
                        telefono = numTel
                    }
                };
            }
            else
            {
                resultadoFinal = new
                {
                    estado = noRegistrado
                };
            }

            return Ok(resultadoFinal);
        }
    }
}
