using TalkToApi.Database;
using TalkToApi.V1.Models;
using TalkToApi.V1.Repositories.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TalkToApi.V1.Repositories
{
    public class TokenRepository : ITokenRepository
    {
        private readonly TalkToContext _banco;

        public TokenRepository(TalkToContext banco)
        {
            _banco = banco;
        }

        public Token Obter(string refresToken)
        {
            return _banco.Token.FirstOrDefault(t => t.RefreshToken == refresToken && t.Utilizado == false);
        }

        public void Cadastrar(Token token)
        {
            _banco.Token.Add(token);
            _banco.SaveChanges();
        }

        public void Atualizar(Token token)
        {
            var model = _banco.Token.Find(token.Id);
            _banco.Token.Update(model);
            _banco.SaveChanges();
        }


    }
}
