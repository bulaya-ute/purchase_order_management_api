using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;

namespace PurchaseOrderManagement.Api.Services;

/// <inheritdoc />
public class RoleHierarchyService : IRoleHierarchyService
{
    private readonly AppDbContext _db;

    public RoleHierarchyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int?> GetCeilingRoleIdAsync(int userId, CancellationToken cancellationToken)
    {
        var map = await LoadParentMapAsync(cancellationToken);

        var heldRoleIds = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        int? ceilingRoleId = null;
        var smallestDepth = int.MaxValue;

        foreach (var roleId in heldRoleIds)
        {
            // Only consider roles that still exist (not soft-deleted) in the map.
            if (!map.ContainsKey(roleId))
            {
                continue;
            }

            var depth = DepthFromRoot(roleId, map);
            if (depth < smallestDepth)
            {
                smallestDepth = depth;
                ceilingRoleId = roleId;
            }
        }

        return ceilingRoleId;
    }

    public async Task<IReadOnlyList<int>> GetAllowedParentRoleIdsAsync(int userId, CancellationToken cancellationToken)
    {
        var ceilingRoleId = await GetCeilingRoleIdAsync(userId, cancellationToken);
        if (ceilingRoleId is null)
        {
            return Array.Empty<int>();
        }

        var map = await LoadParentMapAsync(cancellationToken);
        return CeilingAndDescendants(ceilingRoleId.Value, map);
    }

    /// <summary>
    /// Loads every (non-deleted) role's id -> parent id, once, so chain/subtree walks happen in
    /// memory without N+1 queries. Suitable for the occasional role-management actions this drives.
    /// </summary>
    private async Task<Dictionary<int, int?>> LoadParentMapAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.Roles
            .Select(r => new { r.Id, r.ParentRoleId })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Id, r => r.ParentRoleId);
    }

    /// <summary>
    /// Walks ParentRoleId to the root, counting hops (root = 0). Guards against a malformed cycle
    /// in the data by capping the walk at the number of known roles.
    /// </summary>
    private static int DepthFromRoot(int roleId, IReadOnlyDictionary<int, int?> parentMap)
    {
        var depth = 0;
        var current = roleId;
        var guard = parentMap.Count + 1;

        while (parentMap.TryGetValue(current, out var parentId) && parentId.HasValue)
        {
            depth++;
            current = parentId.Value;
            if (--guard < 0)
            {
                break;
            }
        }

        return depth;
    }

    /// <summary>
    /// Returns the ceiling role id plus all descendants, via a breadth-first walk of the parent map.
    /// </summary>
    private static IReadOnlyList<int> CeilingAndDescendants(int ceilingRoleId, IReadOnlyDictionary<int, int?> parentMap)
    {
        // Build child adjacency from the parent map.
        var childrenByParent = new Dictionary<int, List<int>>();
        foreach (var (id, parentId) in parentMap)
        {
            if (parentId.HasValue)
            {
                if (!childrenByParent.TryGetValue(parentId.Value, out var list))
                {
                    list = new List<int>();
                    childrenByParent[parentId.Value] = list;
                }

                list.Add(id);
            }
        }

        var result = new List<int>();
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(ceilingRoleId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            result.Add(current);

            if (childrenByParent.TryGetValue(current, out var children))
            {
                foreach (var child in children)
                {
                    queue.Enqueue(child);
                }
            }
        }

        return result;
    }
}
