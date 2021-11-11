using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TalkToApi.Database;
using TalkToApi.V1.Models;
using TalkToApi.V1.Repositories.Contracts;

namespace TalkToApi.V1.Repositories
{
    public class MensagemRepository : IMensagemRepository
    {
        private readonly TalkToContext _banco;

        public MensagemRepository(TalkToContext banco)
        {
            _banco = banco;
        }

        public Mensagem Obter(int id)
        {
            return _banco.Mensagens.Find(id);
        }

        public List<Mensagem> ObterMensagem(string usuarioUmId, string usuarioDoisId)
        {
            return _banco.Mensagens.Where(a => (a.DeId == usuarioUmId || a.DeId == usuarioDoisId) && (a.ParaId == usuarioUmId || a.ParaId == usuarioDoisId)).ToList();
        }

        public void Cadastrar(Mensagem mensagem)
        {
            _banco.Mensagens.Add(mensagem);
            _banco.SaveChanges();
        }

        public void Atualizar(Mensagem mensagem)
        {
            _banco.Mensagens.Update(mensagem);
            _banco.SaveChanges();
        }


    }
}
