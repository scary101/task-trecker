using Microsoft.EntityFrameworkCore;
using steptreck.API.Models;
using steptreck.Domain.DTOs.PlanDTOs;
using steptreck.Domain.DTOs.SubscriptionsDTOs;

namespace steptreck.API.Services.Subscriptions
{
    public class PlanServise
    {
        private readonly AppDbContext _context;

        public PlanServise(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PlanPublicDto>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Plans
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => new PlanPublicDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Currency = p.Currency,
                    BasePriceCents = p.BasePriceCents,
                    MaxMembers = p.MaxUsers,
                    MaxProjects = p.MaxProjects,
                    MaxTeams = p.MaxTeams,
                    AllowInvites = p.AllowInvites,
                    AllowNewProjects = p.AllowNewProjects,
                    AllowNewTeams = p.AllowNewTeams,
                    MinMembers = p.MinUsers,
                    MinProjects = p.MinProjects,
                    MinTeams = p.MinTeams,
                    
                })
                .ToListAsync(ct);
        }

        public async Task<PlanPublicDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Plans
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new PlanPublicDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Currency = p.Currency,
                    BasePriceCents = p.BasePriceCents,
                    MaxMembers = p.MaxUsers,
                    MaxProjects = p.MaxProjects,
                    MaxTeams = p.MaxTeams,
                    AllowInvites = p.AllowInvites,
                    AllowNewProjects = p.AllowNewProjects,
                    AllowNewTeams = p.AllowNewTeams,
                    MinMembers = p.MinUsers,
                    MinProjects = p.MinProjects,
                    MinTeams = p.MinTeams,
                })
                .FirstOrDefaultAsync(ct);
        }
    }
}
