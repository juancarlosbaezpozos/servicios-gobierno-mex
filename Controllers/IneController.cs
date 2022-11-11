using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwoCaptcha.Captcha;

namespace AltergoAPI.Nss.Core.Controllers
{
    [SwaggerTag("APIS INE")]
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class IneController : ControllerBase
    {
        private const string page_url = "https://listanominal.ine.mx/scpln/";
        private const string page_url_response = "https://listanominal.ine.mx/scpln/resultado.html";
        private const string site_key = "6LdAe1sUAAAAACrdhVFHK5KmZ5TA8ZJ0iWQ6i64b";
        private const string two_access_token = "--PONER AQUI EL TOKEN--";  //TODO: aqui debes poner el token de tu cuenta en 2captcha
        private const string si_vota = "si-vota-resultado.png";
        private const string no_vota = "no-vota.jpg";
        private const string datos_incorrectos = "resultado-datosincorrectos.png";
        private const string agente = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36";

        /// <summary>
        /// Valida una credencial de elector para votar de tipo C
        /// </summary>
        /// <param name="ClaveElector" example="MLVLCR80042609M900">Clave de Elector a 18 caracteres alfanuméricos</param>
        /// <param name="Ocr" example="0978119955657">Código OCR</param>
        /// <param name="NumeroEmision" example="00">Número de Emisión de la credencial</param>
        /// <returns>Si esta vigente y puede votar</returns>
        /// <response code="200">Respuesta Valida</response>
        /// <response code="400">Respuesta Inválida: La clave de elector no tiene el formato correcto</response>
        /// <response code="404">Respuesta Inválida: No fue posible validar la credencial</response>
        [HttpGet("ValidarIdTipoC")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PostTypeC(
            [FromQuery, Required] string ClaveElector,
            [FromQuery, Required] string Ocr,
            [FromQuery, Required] string NumeroEmision)
        {
            if (string.IsNullOrWhiteSpace(ClaveElector))
            {
                const string err = "La clave de elector es requerida";
                return BadRequest(err);
            }

            if (string.IsNullOrWhiteSpace(Ocr))
            {
                const string err = "El Ocr es requerido";
                return BadRequest(err);
            }

            if (string.IsNullOrWhiteSpace(NumeroEmision))
            {
                const string err = "El número de emisión es requerido";
                return BadRequest(err);
            }

            const string clvPattern = @"^([A-Z&]|[a-z&]{1})([A-Z&]|[a-z&]{1})([A-Z&]|[a-z&]{1})([A-Z&]|[a-z&]{1})([A-Z&]|[a-z&]{1})([A-Z&]|[a-z&]{1})([0-9]{8})([HM]|[hm]{1})([0-9]{3})$";
            var rgPattern = Regex.Match(ClaveElector, clvPattern, RegexOptions.IgnoreCase);
            if (!rgPattern.Success)
            {
                const string err = "La clave de elector no tiene el formato correcto";
                return BadRequest(err);
            }

            const string ocrPattern = @"^([0-9]{13})$";
            var rgOcrPattern = Regex.Match(Ocr, ocrPattern, RegexOptions.IgnoreCase);
            if (!rgOcrPattern.Success)
            {
                const string err = "El ocr no tiene el formato correcto";
                return BadRequest(err);
            }

#error "Debe cambiar el two_access_token por el suyo"
            var solver = new TwoCaptcha.TwoCaptcha(two_access_token);
            var bal = solver.Balance().Result;
            if (bal <= 0)
            {
                const string err = "No hay balance para resolver captcha";
                return BadRequest(err);
            }

            var captcha = new ReCaptcha();
            captcha.SetSiteKey(site_key);
            captcha.SetUrl(page_url);

            solver.Solve(captcha).Wait();
            var gresponse = captcha.Code;
            var restClient = new RestClient(page_url_response)
            {
                Timeout = -1
            };

            var request = new RestRequest(Method.POST);
            request.AddHeader("Upgrade-Insecure-Requests", "1");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            request.AddParameter("g-recaptcha-response", gresponse);
            request.AddParameter("modelo", "c");
            request.AddParameter("numeroEmision", NumeroEmision);
            request.AddParameter("ocr", Ocr);
            request.AddParameter("claveElector", ClaveElector);

            restClient.UserAgent = agente;
            var response = await restClient.ExecuteAsync(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = response.Content;
                var parser = new HtmlParser();
                var document = parser.ParseDocument(response.Content);
                var strongs = document.QuerySelectorAll("tbody tr")
                    .Select(x => new
                    {
                        Data = x.TextContent
                    });

                var details = new Dictionary<string, string>();
                foreach (var s in strongs)
                {
                    var row = s.Data.Replace("\t", "").Trim().Split("\n");
                    details.Add(row[0], row[1]);
                }

                var motivo = string.Empty;
                var flag = false;
                if (result.Contains(si_vota))
                {
                    flag = true;
                }
                else if (result.Contains(datos_incorrectos))
                {
                    motivo = "Datos incorrectos";
                }
                else if (result.Contains(no_vota))
                {
                    motivo = "Credencial vencida";
                }

                object o = new
                {
                    estaVigente = flag ? "Si" : "No",
                    details,
                    motivo
                };

                return Ok(o);
            }

            return NotFound("No fue posible validar la crendencial");
        }//Fin

        /// <summary>
        /// Valida una credencial de elector para votar de tipo E
        /// </summary>
        /// <param name="Cic" example="205147577">Código de identificación de la credencial</param>
        /// <param name="IdCiudadano" example="027305775">Identificador ciudadano</param>
        /// <returns>Si esta vigente y puede votar</returns>
        /// <response code="200">Respuesta Valida</response>
        /// <response code="400">Respuesta Inválida</response>
        /// <response code="404">Respuesta Inválida: No fue posible validar la credencial</response>
        [HttpGet("ValidarIdTipoE")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PostTypeE(
            [FromQuery, Required] string Cic,
            [FromQuery, Required] string IdCiudadano)
        {
            if (string.IsNullOrWhiteSpace(Cic))
            {
                const string err = "Los datos del cic son requeridos";
                return BadRequest(err);
            }

            if (string.IsNullOrWhiteSpace(IdCiudadano))
            {
                const string err = "Los datos del id ciudadano son requeridos";
                return BadRequest(err);
            }

            const string clvPattern = @"^([0-9]{9})$";
            var rgPattern = Regex.Match(Cic, clvPattern, RegexOptions.IgnoreCase);
            if (!rgPattern.Success)
            {
                const string err = "El cic no tiene el formato correcto";
                return BadRequest(err);
            }

            const string ocrPattern = @"^([0-9]{9})$";
            var rgOcrPattern = Regex.Match(IdCiudadano, ocrPattern, RegexOptions.IgnoreCase);
            if (!rgOcrPattern.Success)
            {
                const string err = "El id ciudadano no tiene el formato correcto";
                return BadRequest(err);
            }

            var solver = new TwoCaptcha.TwoCaptcha(two_access_token);
            var bal = solver.Balance().Result;
            if (bal <= 0)
            {
                const string err = "No hay balance para resolver captcha";
                return BadRequest(err);
            }

            var captcha = new ReCaptcha();
            captcha.SetSiteKey(site_key);
            captcha.SetUrl(page_url);

            solver.Solve(captcha).Wait();
            var gresponse = captcha.Code;
            var restClient = new RestClient(page_url_response)
            {
                Timeout = -1
            };

            var request = new RestRequest(Method.POST);
            request.AddHeader("Upgrade-Insecure-Requests", "1");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            request.AddParameter("g-recaptcha-response", gresponse);
            request.AddParameter("modelo", "e");
            request.AddParameter("cic", Cic);
            request.AddParameter("idCiudadano", IdCiudadano);

            restClient.UserAgent = agente;
            var response = await restClient.ExecuteAsync(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = response.Content;

                //buscar aqui en el dom
                var parser = new HtmlParser();
                var document = parser.ParseDocument(response.Content);
                var strongs = document.QuerySelectorAll("tbody tr")
                    .Select(x => new
                    {
                        Data = x.TextContent
                    });

                var details = new Dictionary<string, string>();
                foreach (var s in strongs)
                {
                    var row = s.Data.Replace("\t", "").Trim().Split("\n");
                    details.Add(row[0], row[1]);
                }

                var motivo = string.Empty;
                var flag = false;
                if (result.Contains(si_vota))
                {
                    flag = true;
                }
                else if (result.Contains(datos_incorrectos))
                {
                    motivo = "Datos incorrectos";
                }
                else if (result.Contains(no_vota))
                {
                    motivo = "Credencial vencida";
                }

                object o = new
                {
                    estaVigente = flag ? "Si" : "No",
                    details,
                    motivo
                };

                return Ok(o);
            }

            return NotFound("No fue posible validar la crendencial");
        }//Fin
        

    }
}
