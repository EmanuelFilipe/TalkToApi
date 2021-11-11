using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TalkToApi.V1.Models;
using TalkToApi.V1.Models.DTO;

namespace TalkToApi.Helpers
{
    public class DTOMapperProfile : Profile
    {
        public DTOMapperProfile()
        {
            // faz com que o 'FullName' receba o valor de 'Nome', por causa dos nomes serem diferentes
            CreateMap<ApplicationUser, UsuarioDTO>()
                .ForMember(dest => dest.Nome, orig => orig.MapFrom(propOrig => propOrig.FullName));

            //CreateMap<List<ApplicationUser>, List<UsuarioDTO>>();
        }
    }
}
