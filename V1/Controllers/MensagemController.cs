using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using TalkToApi.V1.Models;
using TalkToApi.V1.Models.DTO;
using TalkToApi.V1.Repositories.Contracts;

namespace TalkToApi.V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class MensagemController : ControllerBase
    {
        private readonly IMensagemRepository _mensagemRepository;
        private readonly IMapper _mapper;

        public MensagemController(IMensagemRepository mensagemRepository, IMapper mapper)
        {
            _mensagemRepository = mensagemRepository;
            _mapper = mapper;
        }

        [Authorize]
        [HttpGet("{usuarioUmId}/{usuarioDoisId}", Name = "Obter")]
        public ActionResult Obter(string usuarioUmId, string usuarioDoisId)
        {
            if (usuarioUmId == usuarioDoisId) return UnprocessableEntity();

            var mensagens = _mensagemRepository.ObterMensagem(usuarioUmId, usuarioDoisId);
            var listaMsg = _mapper.Map<List<Mensagem>, List<MensagemDTO>>(mensagens);
            var lista = new ListaDTO<MensagemDTO>() { Lista = listaMsg };
            lista.Links.Add(new LinkDTO("_self", Url.Link("Obter", new { usuarioUmId, usuarioDoisId }), "GET"));

            return Ok(lista);
        }

        [Authorize]
        [HttpPost("", Name = "Cadastrar")]
        public ActionResult Cadastrar([FromBody]Mensagem mensagem)
        {
            if (ModelState.IsValid)
            {
                _mensagemRepository.Cadastrar(mensagem);

                var mensagemDB = _mapper.Map<Mensagem, MensagemDTO>(mensagem);
                mensagemDB.Links.Add(new LinkDTO("_self", Url.Link("Cadastrar", null), "POST"));
                mensagemDB.Links.Add(new LinkDTO("_atualizacaoParcial", Url.Link("AtualizacaoParcial", new { id = mensagem.Id }), "PATCH"));

                return Ok(mensagemDB);
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
        [HttpPatch("{id}", Name = "AtualizacaoParcial")]
        public ActionResult AtualizacaoParcial (int id, [FromBody]JsonPatchDocument<Mensagem> jsonPatch)
        {
            if (jsonPatch == null) return BadRequest();

            var mensagem = _mensagemRepository.Obter(id);

            jsonPatch.ApplyTo(mensagem);
            mensagem.Atualizado = DateTime.UtcNow;

            _mensagemRepository.Atualizar(mensagem);

            var mensagemDB = _mapper.Map<Mensagem, MensagemDTO>(mensagem);
            mensagemDB.Links.Add(new LinkDTO("_self", Url.Link("AtualizacaoParcial", new { id = mensagem.Id }), "PATCH"));

            return Ok(mensagemDB);
        }

    }
}
