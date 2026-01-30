using AutoMapper;
using VoidPulse.Application.DTOs.Agents;
using VoidPulse.Application.DTOs.Retention;
using VoidPulse.Application.DTOs.SavedFilters;
using VoidPulse.Application.DTOs.Tenants;
using VoidPulse.Application.DTOs.Traffic;
using VoidPulse.Application.DTOs.Users;
using VoidPulse.Domain.Entities;

namespace VoidPulse.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Tenant mappings
        CreateMap<Tenant, TenantResponse>();
        CreateMap<CreateTenantRequest, Tenant>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true));

        // User mappings
        CreateMap<User, UserResponse>()
            .ForMember(dest => dest.Roles,
                opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.Role.Name).ToList()));

        // AgentKey mappings
        CreateMap<AgentKey, AgentKeyResponse>()
            .ForMember(dest => dest.ApiKey, opt => opt.Ignore()); // API key only shown on create

        // TrafficFlow mappings
        CreateMap<TrafficFlow, TrafficFlowResponse>();
        CreateMap<HttpMetadata, HttpMetadataResponse>();

        // RetentionPolicy mappings
        CreateMap<RetentionPolicy, RetentionPolicyResponse>();

        // SavedFilter mappings
        CreateMap<SavedFilter, SavedFilterResponse>();
    }
}
