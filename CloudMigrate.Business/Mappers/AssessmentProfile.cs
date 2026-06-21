using System.Text.Json;
using AutoMapper;
using CloudMigrate.DomainObjects.DBOs;
using CloudMigrate.DomainObjects.DTOs;

public class AssessmentProfile : Profile
{
    public AssessmentProfile()
    {
        CreateMap<Assessment, AssessmentResponse>()
            .ForMember(
                dest => dest.RecommendedServices,
                opt => opt.MapFrom(src =>
                    JsonSerializer.Deserialize<List<string>>(src.RecommendedServicesJson)!))
            .ForMember(
                dest => dest.Risks,
                opt => opt.MapFrom(src =>
                    JsonSerializer.Deserialize<List<string>>(src.RisksJson)!))
            .ForMember(
                dest => dest.Tradeoffs,
                opt => opt.MapFrom(src =>
                    JsonSerializer.Deserialize<List<string>>(src.TradeoffsJson)!))
            .ForMember(
                dest => dest.Roadmap,
                opt => opt.MapFrom(src =>
                    JsonSerializer.Deserialize<List<string>>(src.RoadmapJson)!));
    }
}