using DevOpsProject.Bms.Logic.Data;
using DevOpsProject.Bms.Logic.Services.Interfaces;
using DevOpsProject.Shared.Models;
using DevOpsProject.Shared.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevOpsProject.Bms.Logic.Services;

public class EwZoneService : IEwZoneService
    {
        private readonly BmsDbContext _db;

        public EwZoneService(BmsDbContext db)
        {
            _db = db;
        }

        public async Task UpsertZoneAsync(InterferenceModel model, CancellationToken ct = default)
        {
            var entity = await _db.EwZones.FindAsync(new object[] { model.Id }, ct);

            if (entity == null)
            {
                entity = new EwZone
                {
                    Id = model.Id,
                    CenterLatitude  = model.Location.Latitude,
                    CenterLongitude = model.Location.Longitude,
                    RadiusKm        = model.RadiusKM,
                    IsActive        = true,
                    ActiveFromUtc   = model.CreatedAt.ToUniversalTime(),
                    ActiveToUtc     = null
                };
                _db.EwZones.Add(entity);
            }
            else
            {
                entity.CenterLatitude  = model.Location.Latitude;
                entity.CenterLongitude = model.Location.Longitude;
                entity.RadiusKm        = model.RadiusKM;
                entity.IsActive        = true;
                entity.ActiveToUtc     = null;
            }

            _db.EwZoneHistory.Add(new EwZoneHistory
            {
                ZoneId          = entity.Id,
                CenterLatitude  = entity.CenterLatitude,
                CenterLongitude = entity.CenterLongitude,
                RadiusKm        = entity.RadiusKm,
                IsActive        = entity.IsActive,
                ChangedAtUtc    = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
        }

        public async Task DeactivateZoneAsync(Guid interferenceId, CancellationToken ct = default)
        {
            var entity = await _db.EwZones.FindAsync(new object[] { interferenceId }, ct);
            if (entity == null)
                return;

            entity.IsActive = false;
            entity.ActiveToUtc = DateTime.UtcNow;

            _db.EwZoneHistory.Add(new EwZoneHistory
            {
                ZoneId          = entity.Id,
                CenterLatitude  = entity.CenterLatitude,
                CenterLongitude = entity.CenterLongitude,
                RadiusKm        = entity.RadiusKm,
                IsActive        = entity.IsActive,
                ChangedAtUtc    = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
        }

        public Task<List<EwZone>> GetActiveZonesAsync(CancellationToken ct = default)
            => _db.EwZones.Where(z => z.IsActive).ToListAsync(ct);

        public Task<List<EwZoneHistory>> GetZonesHistoryAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
        {
            var q = _db.EwZoneHistory.AsQueryable();

            if (fromUtc.HasValue)
                q = q.Where(z => z.ChangedAtUtc >= fromUtc.Value);

            if (toUtc.HasValue)
                q = q.Where(z => z.ChangedAtUtc <= toUtc.Value);

            return q.ToListAsync(ct);
        }
    }