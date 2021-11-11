using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using TalkToApi.V1.Models;
using TalkToApi.V1.Models.DTO;
using TalkToApi.V1.Repositories.Contracts;

namespace TalkToApi.V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public UsuarioController(IUsuarioRepository usuarioRepository, ITokenRepository tokenRepository,
                                 SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager,
                                 IMapper mapper)
        {
            _usuarioRepository = usuarioRepository;
            _tokenRepository = tokenRepository;
            _signInManager = signInManager;
            _userManager = userManager;
            _mapper = mapper;
        }

        [Authorize]
        [HttpGet("", Name = "ObterTodos")]
        public ActionResult ObterTodos()
        {
            var usuarioAppUser = _userManager.Users.ToList();

            var listaUsuarioDTO = _mapper.Map<List<ApplicationUser>, List<UsuarioDTO>>(usuarioAppUser);

            foreach (var usuario in listaUsuarioDTO)
                usuario.Links.Add(new LinkDTO("_self", Url.Link("ObterUsuario", new { id = usuario.Id}), "GET"));

            var lista = new ListaDTO<UsuarioDTO>() { Lista = listaUsuarioDTO };
            lista.Links.Add(new LinkDTO("_self", Url.Link("ObterTodos", null), "GET"));

            return Ok(lista);
        }

        [Authorize]
        [HttpGet("{id}", Name = "ObterUsuario")]
        public ActionResult ObterUsuario(string id)
        {
            var usuario = _userManager.FindByIdAsync(id).Result;

            if (usuario == null) return NotFound();

            var usuarioDB = _mapper.Map<ApplicationUser, UsuarioDTO>(usuario);
            usuarioDB.Links.Add(new LinkDTO("_self", Url.Link("ObterUsuario", new { id = usuario.Id }), "GET"));
            usuarioDB.Links.Add(new LinkDTO("_atualizar", Url.Link("Atualizar", new { id = usuario.Id }), "PUT"));

            return Ok(usuarioDB);
        }

        [HttpPost("login")]
        public ActionResult Login([FromBody] UsuarioDTO usuarioDTO)
        {
            //Remove propriedades para validação do modelo
            ModelState.Remove("ConfirmacaoSenha");
            ModelState.Remove("Nome");

            if (ModelState.IsValid)
            {
                var usuario = _usuarioRepository.Obter(usuarioDTO.Email, usuarioDTO.Senha);

                if (usuario != null)
                {
                    //_signInManager.SignInAsync(usuario, false);

                    //Retorna o Token (JWT)
                    var token = BuildToken(usuario);
                    RefreshToken(usuario, token);

                    return Ok(token);
                }
                else
                    return NotFound("Usuário não localizado!");
            }
            else
            {
                return UnprocessableEntity(ModelState);
            }

        }

        [HttpPost("renovar")]
        public ActionResult Renovar([FromBody] TokenDTO tokenDTO)
        {
            var refreshTokenDB = _tokenRepository.Obter(tokenDTO.RefreshToken);

            if (refreshTokenDB == null) return NotFound();

            refreshTokenDB.Atualizado = DateTime.UtcNow;
            refreshTokenDB.Utilizado = true;
            _tokenRepository.Atualizar(refreshTokenDB);

            var usuario = _usuarioRepository.Obter(refreshTokenDB.UsuarioId);
            var token = BuildToken(usuario);

            RefreshToken(usuario, token);

            return Ok(token);
        }

        [HttpPost("", Name = "Cadastar")]
        public ActionResult Cadastrar([FromBody] UsuarioDTO usuarioDTO)
        {
            if (ModelState.IsValid)
            {
                var usuario = new ApplicationUser()
                {
                    FullName = usuarioDTO.Nome,
                    UserName = usuarioDTO.Email,
                    Email = usuarioDTO.Email,
                };

                var resultado = _userManager.CreateAsync(usuario, usuarioDTO.Senha).Result;

                if (!resultado.Succeeded)
                {
                    List<string> erros = new List<string>();

                    foreach (var erro in resultado.Errors)
                        erros.Add(erro.Description);

                    return UnprocessableEntity(erros);
                }
                else
                {
                    var usuarioDB = _mapper.Map<ApplicationUser, UsuarioDTO>(usuario);
                    usuarioDB.Links.Add(new LinkDTO("_self", Url.Link("Cadastrar", new { id = usuario.Id }), "POST"));
                    usuarioDB.Links.Add(new LinkDTO("_obter", Url.Link("ObterUsuario", new { id = usuario.Id }), "GET"));
                    usuarioDB.Links.Add(new LinkDTO("_atualizar", Url.Link("Atualizar", new { id = usuario.Id }), "PUT"));

                    return Ok(usuarioDB);
                }
            }
            else
            {
                return UnprocessableEntity(ModelState);
            }
        }

        [Authorize]
        [HttpPut("{id}", Name = "Atualizar")]
        public ActionResult Atualizar(string id, [FromBody] UsuarioDTO usuarioDTO)
        {
            ApplicationUser usuario = _userManager.GetUserAsync(HttpContext.User).Result;
            if (usuario.Id != id)
                return Forbid();

            if (ModelState.IsValid)
            {
                usuario.FullName = usuarioDTO.Nome;
                usuario.UserName = usuarioDTO.Email;
                usuario.Email = usuarioDTO.Email;
                usuario.Slogan = usuarioDTO.Slogan;

                var resultado = _userManager.UpdateAsync(usuario).Result;
                _userManager.RemovePasswordAsync(usuario);
                _userManager.AddPasswordAsync(usuario, usuarioDTO.Senha);

                if (!resultado.Succeeded)
                {
                    List<string> erros = new List<string>();

                    foreach (var erro in resultado.Errors)
                        erros.Add(erro.Description);

                    return UnprocessableEntity(erros);
                }
                else
                {
                    var usuarioDB = _mapper.Map<ApplicationUser, UsuarioDTO>(usuario);
                    usuarioDB.Links.Add(new LinkDTO("_atualizar", Url.Link("Atualizar", new { id = usuario.Id }), "PUT"));
                    usuarioDB.Links.Add(new LinkDTO("_obter", Url.Link("ObterUsuario", new { id = usuario.Id }), "GET"));

                    return Ok(usuarioDB);
                }
            }
            else
            {
                return UnprocessableEntity(ModelState);
            }
        }

        // Métodos que não fazem parte da documento swagger devem ser do tipo 'Private'
        // caso contrário irá gerar erro
        private TokenDTO BuildToken(ApplicationUser usuario)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("chave-api-jwt-minhas-tarefefas"));
            var sign = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            //expira depois de 1hr
            var exp = DateTime.UtcNow.AddHours(1);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: exp,
                signingCredentials: sign
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var refreshToken = Guid.NewGuid().ToString();
            var expRefreshToken = DateTime.UtcNow.AddHours(2);

            var tokenDTO = new TokenDTO
            {
                Token = tokenString,
                Expiration = exp,
                RefreshToken = refreshToken,
                ExpirationRefreshToken = expRefreshToken
            };


            return tokenDTO;
        }

        private void RefreshToken(ApplicationUser usuario, TokenDTO token)
        {
            var tokenModel = new Token()
            {
                RefreshToken = token.RefreshToken,
                ExpirationToken = token.Expiration,
                ExpirationRefreshToken = token.ExpirationRefreshToken,
                Usuario = usuario,
                Criado = DateTime.UtcNow,
                Utilizado = false
            };

            _tokenRepository.Cadastrar(tokenModel);
        }

    }
}
