﻿using Scripts.Game.Defined.Serialized.Statistics;
using Scripts.Model.SaveLoad;
using Scripts.Model.SaveLoad.SaveObjects;
using Scripts.Model.Stats;
using Scripts.Presenter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Model.Characters {

    public class Stats : IEnumerable<KeyValuePair<StatType, Stat>>, ISaveable<CharacterStatsSave>, IComparable<Stats> {
        public enum Set {
            MOD,
            MOD_UNBOUND,
            MAX
        }

        public enum Get {
            MOD,
            MOD_AND_EQUIP,
            MAX
        }

        public Func<StatType, int> GetEquipmentBonus;
        public Action<SplatDetails> AddSplat;

        private readonly IDictionary<StatType, Stat> dict;

        public int Level;
        public int StatPoints;

        public Stats() {
            this.dict = new Dictionary<StatType, Stat>();
            this.AddSplat = (a => { });
            SetDefaultStats();
            SetDefaultResources();
            GetEquipmentBonus = (st) => 0;
        }

        public IEnumerable<Stat> Resources {
            get {
                return dict.Values.Where(v => StatType.RESOURCES.Contains(v.Type));
            }
        }

        public string LongAttributeDistribution {
            get {
                List<string> assignables = new List<string>();
                List<string> resources = new List<string>();
                List<string> other = new List<string>();
                foreach (KeyValuePair<StatType, Stat> pair in dict) {
                    string s = string.Format("{0} {1}/{2} {3}",
                        pair.Key.Name,
                        pair.Value.Mod,
                        pair.Value.Max,
                        StatType.ASSIGNABLES.Contains(pair.Key) ? string.Format("({0})", Util.Sign(GetEquipmentBonus(pair.Key))) : string.Empty
                        );
                    if (StatType.ASSIGNABLES.Contains(pair.Key)) {
                        assignables.Add(s);
                    } else if (StatType.RESOURCES.Contains(pair.Key)) {
                        resources.Add(s);
                    }
                }
                return string.Format("Level {0}\n<Assignables>\n{1}\n<Resources>\n{2}",
                    this.Level,
                    string.Join("\n", assignables.ToArray()),
                    string.Join("\n", resources.ToArray())
                    );
            }
        }

        public State State {
            get {
                if (GetStatCount(Get.MOD, StatType.HEALTH) <= 0) {
                    return State.DEAD;
                } else {
                    return State.ALIVE;
                }
            }
        }

        public bool CanLevelUp {
            get {
                return GetStatCount(Get.MOD, StatType.EXPERIENCE) >= GetStatCount(Get.MAX, StatType.EXPERIENCE);
            }
        }

        public void AddStat(Stat stat) {
            this.dict.Add(stat.Type, stat);
            AddSplat(new SplatDetails(stat.Type.Color, "+", stat.Type.Sprite));
        }

        protected void RemoveStat(StatType type) {
            this.dict.Remove(type);
        }

        public void SetToStat(StatType statType, Set type, int amount) {
            if (HasStat(statType) && amount != 0) {
                Stat stat = dict[statType];
                if (type == Set.MOD) {
                    stat.Mod = amount;
                } else if (type == Set.MAX) {
                    stat.Max = amount;
                } else if (type == Set.MOD_UNBOUND) {
                    stat.SetMod(amount, false);
                }
            }
            AddSplat(new SplatDetails(statType.DetermineColor(amount), string.Format("={0}", amount), statType.Sprite));
        }

        public void AddToStat(StatType statType, Set type, int amount) {
            if (HasStat(statType) && amount != 0) {
                Stat stat = dict[statType];
                if (type == Set.MOD) {
                    stat.Mod += amount;
                } else if (type == Set.MAX) {
                    stat.Max += amount;
                } else if (type == Set.MOD_UNBOUND) {
                    stat.SetMod(stat.Mod + amount, false);
                }
            }
            AddSplat(new SplatDetails(statType.DetermineColor(amount), StatUtil.ShowSigns(amount), statType.Sprite));
        }

        public bool HasStat(StatType statType) {
            Stat stat;
            dict.TryGetValue(statType, out stat);
            return stat != null;
        }

        public int GetStatCount(Get type, params StatType[] statTypes) {
            int sum = 0;
            foreach (StatType st in statTypes) {
                if (HasStat(st)) {
                    Stat stat;
                    dict.TryGetValue(st, out stat);
                    if (type == Get.MOD) {
                        sum += stat.Mod;
                    } else if (type == Get.MOD_AND_EQUIP) {
                        sum += (stat.Mod + GetEquipmentBonus(st));
                    } else if (type == Get.MAX) {
                        sum += stat.Max;
                    }
                }
            }
            return sum;
        }

        public void Update(Character c) {
            ICollection<Stat> stats = dict.Values;
            foreach (Stat stat in stats) {
                stat.Update(c);
            }
        }

        public override bool Equals(object obj) {
            var item = obj as Stats;

            if (item == null) {
                return false;
            }

            return Util.IsDictionariesEqual<StatType, Stat>(this.dict, item.dict);
        }

        public override int GetHashCode() {
            return 0;
        }

        protected void InitializeStats(int level, int str, int agi, int intel, int vit) {
            this.Level = level;
            SetToBothStat(StatType.STRENGTH, str);
            SetToBothStat(StatType.AGILITY, agi);
            SetToBothStat(StatType.INTELLECT, intel);
            SetToBothStat(StatType.VITALITY, vit);
        }

        public void InitializeResources() {
            ICollection<Stat> stats = dict.Values;
            foreach (Stat stat in stats) {
                if (StatType.RESTORED.Contains(stat.Type)) {
                    stat.Mod = stat.Max;
                }
            }
        }

        private void SetToBothStat(StatType type, int amount) {
            SetToStat(type, Set.MOD_UNBOUND, amount);
            SetToStat(type, Set.MAX, amount);
        }

        private void SetDefaultStats() {
            this.AddStat(new Strength(0, 0));
            this.AddStat(new Agility(0, 0));
            this.AddStat(new Intellect(0, 0));
            this.AddStat(new Vitality(0, 0));
        }

        private void SetDefaultResources() {
            this.AddStat(new Health(0, 0));
        }

        IEnumerator<KeyValuePair<StatType, Stat>> IEnumerable<KeyValuePair<StatType, Stat>>.GetEnumerator() {
            return dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return dict.GetEnumerator();
        }

        public CharacterStatsSave GetSaveObject() {
            List<StatSave> list = new List<StatSave>();
            foreach (KeyValuePair<StatType, Stat> pair in dict) {
                Stat stat = pair.Value;
                list.Add(stat.GetSaveObject());
            }
            return new CharacterStatsSave(this.Level, this.StatPoints, list);
        }

        public void InitFromSaveObject(CharacterStatsSave saveObject) {
            this.Level = saveObject.Level;
            this.StatPoints = saveObject.StatBonusCount;
            dict.Clear();
            foreach (StatSave save in saveObject.Stats) {
                Stat stat = save.CreateObjectFromID();
                stat.InitFromSaveObject(save);
                dict.Add(stat.Type, stat);
            }
        }

        public int CompareTo(Stats other) {
            int diff = StatUtil.GetDifference(StatType.AGILITY, this, other);
            if (diff == 0) {
                return (Util.IsChance(.5) ? -1 : 1);
            } else {
                return diff;
            }
        }
    }
}
