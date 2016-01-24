﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;

/**
 * Spells always consume 100% of the user's charge bar
 * which is not considered to be a cost as it varies
 */
public abstract class Spell : ICloneable, IUndoableProcess {
    public string Name { get; private set; }
    public string Description { get; private set; }
    public SpellType SpellType { get; private set; }
    public TargetType TargetType { get; private set; }
    public bool IsEnabled { get; set; }
    public string CastText { get; set; }
    public SpellResult Result { get; private set; }
    public Character Caster { get; private set; }
    public Character Target { get; private set; }
    public int Damage { get; protected set; }
    Dictionary<ResourceType, int> costs;

    Action IUndoableProcess.UndoAction { get { return new Action(() => Undo()); } }
    Action IProcess.Action { get { return new Action(() => OnSuccess(Caster, Target)); } }

    public Spell(string name, string description, SpellType spellType, TargetType targetType, Dictionary<ResourceType, int> costs) {
        this.Name = name;
        this.Description = description;
        this.SpellType = spellType;
        this.TargetType = targetType;
        this.costs = costs;
        this.IsEnabled = true;
    }

    public Spell(string name, SpellType spellType, TargetType targetType) {
        this.Name = name;
        this.SpellType = spellType;
        this.TargetType = targetType;
        this.costs = new Dictionary<ResourceType, int>();
    }

    public virtual string GetNameAndInfo(Character caster) {
        StringBuilder s = new StringBuilder();
        s.Append(IsCastable(caster) ? Name : Util.Color(Name, Color.red) + (costs.Count == 0 ? "" : " - "));
        List<string> elements = new List<string>();
        foreach (KeyValuePair<ResourceType, int> entry in costs) {
            if (entry.Key != ResourceType.CHARGE) {
                Color resourceColor = ResourceFactory.CreateResource(entry.Key, 0).OverColor;
                int cost = entry.Value;
                elements.Add(Util.Color("" + cost, resourceColor));
            }
        }
        s.Append(string.Join("/", elements.ToArray()));
        return s.ToString();
    }

    public virtual bool IsCastable(Character caster, Character target = null) {
        if (!target.IsTargetable) {
            return false;
        }
        foreach (KeyValuePair<ResourceType, int> resourceCost in costs) {
            if (caster.GetResource(resourceCost.Key) == null || caster.GetResource(resourceCost.Key).False < resourceCost.Value) {
                return false;
            }
        }
        return caster.GetResource(ResourceType.CHARGE).IsMaxed();
    }

    protected virtual void ConsumeResources(Character caster) {
        foreach (KeyValuePair<ResourceType, int> resourceCost in costs) {
            caster.GetResource(resourceCost.Key).False -= resourceCost.Value;
        }
        caster.GetResource(ResourceType.CHARGE).clearFalse();
    }

    public void TryCast(Character caster, Character target) {
        if (IsCastable(caster, target)) {
            ConsumeResources(caster);
            Cast(caster, target);
            caster.AddToCastSpellHistory((Spell)Clone());
            target.AddToRecievedSpellHistory((Spell)Clone());
        } else {
            Result = SpellResult.CANT_CAST;
        }
    }

    void Cast(Character caster, Character target) {
        if (Util.Chance(CalculateHitRate(caster, target))) {
            this.Caster = caster;
            this.Target = target;
            OnSuccess(caster, target);

            Result = SpellResult.HIT;
        } else {
            OnFailure(caster, target);
            Result = SpellResult.MISS;
        }
    }

    public override bool Equals(object obj) {
        // If parameter is null return false.
        if (obj == null) {
            return false;
        }

        // If parameter cannot be cast to Page return false.
        Spell s = obj as Spell;
        if ((object)s == null) {
            return false;
        }

        // Return true if the fields match:
        return this.Name.Equals(s.Name);
    }

    public override int GetHashCode() {
        return Name.GetHashCode();
    }

    public object Clone() {
        return this.MemberwiseClone();
    }

    void IProcess.Play() {
        OnSuccess(Caster, Target);
    }

    void IUndoableProcess.Undo() {
        Undo();
    }

    public abstract double CalculateHitRate(Character caster, Character target);
    public abstract int CalculateDamage(Character caster, Character target);
    protected abstract void OnSuccess(Character caster, Character target);
    protected abstract void OnFailure(Character caster, Character target);
    public abstract void Undo();
}
