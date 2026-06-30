using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Assets.Scripts.Utilities
{
    public static class IdGenerator
    {
        public enum IdType
        {
            Unit,
            Skill,
            Item,
            Tile
        }

        private static long _nextId = 0;

        public static string GenerateId(IdType type)
        {
            string prefix = type switch
            {
                IdType.Unit => "unit",
                IdType.Skill => "skill",
                IdType.Item => "item",
                IdType.Tile => "tile",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            long idPart = Interlocked.Increment(ref _nextId);

            return $"{prefix}_{idPart}";
        }
    }
}
