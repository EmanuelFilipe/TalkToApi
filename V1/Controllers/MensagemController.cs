using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TalkToApi.V1.Models;
using TalkToApi.V1.Repositories.Contracts;

namespace TalkToApi.V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class MensagemController : ControllerBase
    {
        private readonly IMensagemRepository _mensagemRepository;

        public MensagemController(IMensagemRepository mensagemRepository)
        {
            _mensagemRepository = mensagemRepository;
        }

        [Authorize]
        [HttpGet("{usuarioUmId}/{usuarioDoisId}")]
        public ActionResult Obter(string usuarioUmId, string usuarioDoisId)
        {
            if (usuarioUmId == usuarioDoisId) return UnprocessableEntity();

            return Ok(_mensagemRepository.ObterMensagem(usuarioUmId, usuarioDoisId));
        }

        [Authorize]
        [HttpPost]
        public ActionResult Cadastrar([FromBody]Mensagem mensagem)
        {
            if (ModelState.IsValid)
            {
                _mensagemRepository.Cadastrar(mensagem);
                return Ok(mensagem);
            }
            else
            {
                return UnprocessableEntity(ModelState);
            }
        }

        /* 
         * JSONPATCH - [{ "op": "add|remove|replace", "patch": "é o nome do campo, ex: 'Texto', "value": "mensagem substituida!"},
         *              { "op": "add|remove|replace", "patch": "Excluido", "value": true}]
         */
        [Authorize]
        [HttpPatch("{id}")]
        public ActionResult AtualizacaoParcial (int id, [FromBody]JsonPatchDocument<Mensagem> jsonPatch)
        {
            if (jsonPatch == null) return BadRequest();

            var mensagem = _mensagemRepository.Obter(id);

            jsonPatch.ApplyTo(mensagem);
            mensagem.Atualizado = DateTime.UtcNow;

            _mensagemRepository.Atualizar(mensagem);

            return Ok(mensagem);
        }

    }
}
