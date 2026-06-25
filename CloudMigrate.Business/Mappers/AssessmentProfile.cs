using System.Text.Json;
using AutoMapper;
using CloudMigrate.DomainObjects.DBOs;
using CloudMigrate.DomainObjects.DTOs;

namespace CloudMigrate.Business.Mappers;

public class AssessmentProfile : Profile
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    public AssessmentProfile()
    {
        CreateMap<Assessment, AssessmentResponse>()
            .ForMember(dest => dest.RecommendedServices,
                opt => opt.MapFrom(src =>
                    JsonSerializer.Deserialize<List<string>>(src.RecommendedServicesJson, _jsonOptions) ?? new()))
            .ForMember(dest => dest.Risks,
                opt => opt.MapFrom(src =>
                    JsonSerializer.Deserialize<List<string>>(src.RisksJson, _jsonOptions) ?? new()))
            .ForMember(dest => dest.Tradeoffs,
                opt => opt.MapFrom(src =>
                    JsonSerializer.Deserialize<List<string>>(src.TradeoffsJson, _jsonOptions) ?? new()))
            .ForMember(dest => dest.Roadmap,
                opt => opt.MapFrom(src =>
                    JsonSerializer.Deserialize<List<string>>(src.RoadmapJson, _jsonOptions) ?? new()));

        CreateMap<AssessmentResponse, Assessment>()
            .ForMember(dest => dest.RecommendedServicesJson,
                opt => opt.MapFrom(src => JsonSerializer.Serialize(src.RecommendedServices)))
            .ForMember(dest => dest.RisksJson,
                opt => opt.MapFrom(src => JsonSerializer.Serialize(src.Risks)))
            .ForMember(dest => dest.TradeoffsJson,
                opt => opt.MapFrom(src => JsonSerializer.Serialize(src.Tradeoffs)))
            .ForMember(dest => dest.RoadmapJson,
                opt => opt.MapFrom(src => JsonSerializer.Serialize(src.Roadmap)))
            .ForMember(dest => dest.Id,        opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDateTime, opt => opt.Ignore());
    }
}