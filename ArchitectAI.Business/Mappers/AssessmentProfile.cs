using System.Text.Json;
using AutoMapper;
using ArchitectAI.DomainObjects.DBOs;
using ArchitectAI.DomainObjects.DTOs;

namespace ArchitectAI.Business.Mappers;

public class AssessmentProfile : Profile
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    public AssessmentProfile()
    {
        // Assessment to AssessmentResponse
        CreateMap<Assessment, AssessmentResponse>()
            .ForMember(dest => dest.ExecutiveSummary,
                opt => opt.MapFrom(src => src.ExecutiveSummary))
            .ForMember(dest => dest.RecommendedServices,
                opt => opt.MapFrom(src => src.RecommendedServices))
            .ForMember(dest => dest.Risks,
                opt => opt.MapFrom(src => src.Risks))
            .ForMember(dest => dest.Tradeoffs,
                opt => opt.MapFrom(src => src.Tradeoffs))
            .ForMember(dest => dest.Roadmap,
                opt => opt.MapFrom(src => src.Roadmap));
        
    }
}