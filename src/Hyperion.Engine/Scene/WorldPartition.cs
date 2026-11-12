using OpenTK.Mathematics;

namespace Hyperion.Engine.Scene;

internal sealed class WorldPartition(float cellSize = 32f)
{
    private readonly Dictionary<Cell, HashSet<Entity>> _cells = [];
    private readonly Dictionary<Entity, Cell> _entityCells = [];

    public void Track(Entity entity)
    {
        var targetCell = Cell.FromPosition(entity.Transform.Position, cellSize);
        if (_entityCells.TryGetValue(entity, out var currentCell) && currentCell == targetCell)
        {
            return;
        }

        if (_entityCells.Remove(entity, out currentCell)
            && _cells.TryGetValue(currentCell, out var previousEntities))
        {
            previousEntities.Remove(entity);
            if (previousEntities.Count == 0)
            {
                _cells.Remove(currentCell);
            }
        }

        if (!_cells.TryGetValue(targetCell, out var targetEntities))
        {
            targetEntities = [];
            _cells.Add(targetCell, targetEntities);
        }

        targetEntities.Add(entity);
        _entityCells[entity] = targetCell;
    }

    public IEnumerable<Entity> QuerySphere(Vector3 center, float radius)
    {
        var cellRadius = MathF.Sqrt(3f) * cellSize * 0.5f;
        var maximumDistanceSquared = MathF.Pow(radius + cellRadius, 2f);

        foreach (var (cell, entities) in _cells)
        {
            if ((cell.Center(cellSize) - center).LengthSquared > maximumDistanceSquared)
            {
                continue;
            }

            foreach (var entity in entities)
            {
                yield return entity;
            }
        }
    }

    private readonly record struct Cell(int X, int Y, int Z)
    {
        public static Cell FromPosition(Vector3 position, float size) => new(
            (int)MathF.Floor(position.X / size),
            (int)MathF.Floor(position.Y / size),
            (int)MathF.Floor(position.Z / size));

        public Vector3 Center(float size) => new(
            (X + 0.5f) * size,
            (Y + 0.5f) * size,
            (Z + 0.5f) * size);
    }
}
