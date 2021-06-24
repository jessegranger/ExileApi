using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.Elements.InventoryElements;
using static Follower.Globals;

namespace Follower
{
    public class ItemQuery
    {
        private class ItemModQuery
        {
            public string ModName;
            public bool AcceptAnyValue = true;
            public float AcceptMinValue = float.MinValue;
            public float AcceptMaxValue = float.MaxValue;
            public bool Matches(NormalInventoryItem item)
            {
                return HasMod(item.Item, ModName);
            }
            public ItemModQuery(string modName) => ModName = modName;
        }
        private abstract class ItemQueryGroup
        {
            public abstract bool Matches(NormalInventoryItem item);
        }
        private class ItemQueryGroup_AllMatch : ItemQueryGroup
        {
            private List<ItemModQuery> matches;
            public ItemQueryGroup_AllMatch(params ItemModQuery[] query) => matches = new List<ItemModQuery>(query);
            public override bool Matches(NormalInventoryItem item) => matches.All((query) => query.Matches(item));
        }
        private class ItemQueryGroup_CountMatch : ItemQueryGroup
        {

            public ItemQueryGroup_CountMatch(int count, ItemModQuery[] q)
            {
                matchCount = count;
                matches = new List<ItemModQuery>(q);
            }

            private int matchCount;
            private List<ItemModQuery> matches;

            public override bool Matches(NormalInventoryItem item) => matches.Where((query) => query.Matches(item)).Count() >= matchCount;
        }
        private class ItemQueryGroup_NotMatch : ItemQueryGroup
        {
            private List<ItemModQuery> matches = new List<ItemModQuery>();
            public override bool Matches(NormalInventoryItem item) => matches.All((query) => !query.Matches(item));
        }

        private List<ItemQueryGroup> groups = new List<ItemQueryGroup>();
        public bool Matches(NormalInventoryItem item)
        {
            return groups.All((group) => group.Matches(item));
        }
        private void MatchAll(params ItemModQuery[] query)
        {
            groups.Add(new ItemQueryGroup_AllMatch(query));
        }
        public void MatchAll(params string[] query)
        {
            ItemModQuery[] q = new ItemModQuery[query.Length];
            for (int i = 0; i < q.Length; i++)
            {
                q[i] = new ItemModQuery(query[i]);
            }
            MatchAll(q);
        }
        public void MatchCount(int count, params string[] query)
        {
            ItemModQuery[] q = new ItemModQuery[query.Length];
            for (int i = 0; i < q.Length; i++)
            {
                q[i] = new ItemModQuery(query[i]);
            }
            groups.Add(new ItemQueryGroup_CountMatch(count, q));
        }
    }
}
