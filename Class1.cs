using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;
using ThunderRoad.AI.Action;

namespace ImagineBreaker
{
    public class ImagineBreakerTKCheck : MonoBehaviour { }
    public class ImagineBreakerSpell : SpellCastData
    {
        EffectInstance instance;
        SpellCaster currentSpellCaster;
        public override void Load(SpellCaster spellCaster, Level level)
        {
            base.Load(spellCaster, level);
            currentSpellCaster = spellCaster;
            spellCaster.ragdollHand.collisionHandler.OnCollisionStartEvent += CollisionHandler_OnCollisionStartEvent;
            spellCaster.ragdollHand.lowerArmPart.collisionHandler.OnCollisionStartEvent += CollisionHandler_OnCollisionStartEvent;
            spellCaster.ragdollHand.OnGrabEvent += RagdollHand_OnGrabEvent;
            spellCaster.ragdollHand.creature.OnDamageEvent += Creature_OnDamageEvent;
        }
        public override void UpdateCaster()
        {
            base.UpdateCaster();
            foreach (Item item in Item.allTk)
            {
                if (item.gameObject.GetComponent<ImagineBreakerTKCheck>() == null) item.gameObject.AddComponent<ImagineBreakerTKCheck>();
            }
            if (currentSpellCaster.ragdollHand.grabbedHandle?.item is Item grabbed)
            {
                bool isImbued = false;
                foreach(Imbue imbue in grabbed.imbues)
                {
                    if (imbue.energy > 0f)
                    {
                        imbue.Stop();
                        isImbued = true;
                    }
                }
                if(isImbued)
                {
                    PlayEffect();
                }
            }
            if(currentSpellCaster.ragdollHand.colliderGroup.imbue != null && currentSpellCaster.ragdollHand.colliderGroup.imbue.energy > 0)
            {
                currentSpellCaster.ragdollHand.colliderGroup.imbue.Stop();
                PlayEffect();
            }
        }

        private void Creature_OnDamageEvent(CollisionInstance collisionInstance, EventTime eventTime)
        {
            bool isMagic = false;
            if (collisionInstance.targetColliderGroup != null && (collisionInstance.targetColliderGroup == currentSpellCaster.ragdollHand.colliderGroup || collisionInstance.targetColliderGroup == currentSpellCaster.ragdollHand.lowerArmPart.colliderGroup))
            {
                if (collisionInstance.casterHand != null && collisionInstance.damageStruct.damageType == DamageType.Energy)
                {
                    collisionInstance.damageStruct.damage = 0;
                    if (!collisionInstance.casterHand.mana.creature.isPlayer && collisionInstance.casterHand.isSpraying && eventTime == EventTime.OnEnd)
                    {
                        collisionInstance.casterHand.mana.creature.TryPush(Creature.PushType.Magic, -(currentSpellCaster.transform.position - collisionInstance.casterHand.transform.position).normalized, 1);
                    }
                    isMagic = true;
                }
                if (collisionInstance.sourceCollider?.GetComponentInParent<Item>() is Item item)
                {
                    if (collisionInstance.sourceColliderGroup?.imbue?.energy > 0f)
                    {
                        if (collisionInstance.damageStruct.damageType == DamageType.Energy)
                        {
                            collisionInstance.damageStruct.damage = 0;
                            collisionInstance.effectInstance = null;
                        }
                        if (eventTime == EventTime.OnStart)
                        {
                            collisionInstance.sourceColliderGroup.imbue.Stop();
                            /*List<EffectDecal> decals = new List<EffectDecal>();
                            foreach (Effect effect in collisionInstance.effectInstance.effects)
                            {
                                if (effect.GetType() == typeof(EffectDecal)) decals.Add(effect as EffectDecal);
                                effect.Stop();
                            }
                            for (int i = 0; i < decals.Count; i++)
                            {
                                decals[i].Despawn();
                                decals.RemoveAt(i);
                                i--;
                            }
                            decals.Clear();*/
                        }
                        isMagic = true;
                    }
                    if (item.GetComponent<ItemMagicProjectile>() != null || item.GetComponent<ItemMagicAreaProjectile>() != null)
                    {
                        collisionInstance.damageStruct.damage = 0;
                        //if (item.gameObject.activeSelf && eventTime == EventTime.OnEnd) item.Despawn();
                        isMagic = true;
                    }
                    else if (item.lastHandler == null && item.physicBody.velocity.magnitude >= 2)
                    {
                        if (item.GetComponent<ImagineBreakerTKCheck>() == null) item.Despawn();
                        else
                        {
                            item.physicBody.velocity = Vector3.zero;
                            item.physicBody.angularVelocity = Vector3.zero;
                        }
                        isMagic = true;
                    }
                    if (item.isTelekinesisGrabbed)
                    {
                        foreach (Handle handle in collisionInstance.sourceCollider.GetComponentInParent<Item>().handles)
                        {
                            handle.telekinesisHandler?.telekinesis?.TryRelease(false);
                        }
                        item.physicBody.velocity = Vector3.zero;
                        item.physicBody.angularVelocity = Vector3.zero;
                        isMagic = true;
                    }
                }
            }
            if (isMagic) PlayEffect();
        }

        private void RagdollHand_OnGrabEvent(Side side, Handle handle, float axisPosition, HandlePose orientation, EventTime eventTime)
        {
            bool isMagic = false;
            if (handle.item != null)
            {
                foreach (Imbue imbue in handle.item.imbues)
                {
                    if (imbue.energy > 0f)
                    {
                        imbue.Stop();
                        isMagic = true;
                    }
                }
                foreach (Handle.JointModifier modifier in handle.jointModifiers)
                {
                    if (modifier.handler.GetType() == typeof(SpellCastData))
                    {
                        handle.RemoveJointModifier(modifier.handler);
                        isMagic = true;
                    }
                }
            }
            if(handle.GetComponentInParent<Creature>() is Creature creature)
            {
                if (creature.brain.isElectrocuted)
                {
                    creature.StopShock();
                    isMagic = true;
                }
                creature.ragdoll.StopCoroutine("NoGravityCoroutine");
                foreach (Ragdoll.PhysicModifier modifier in creature.ragdoll.physicModifiers)
                {
                    if (modifier.handler is SpellCastGravity)
                    {
                        creature.ragdoll.RemovePhysicModifier(modifier.handler);
                        isMagic = true;
                    }
                }
                if (handle.GetComponentInParent<RagdollPart>() is RagdollPart part)
                {
                    if(part == creature.handLeft.lowerArmPart && creature.handLeft.caster.spellInstance != null)
                    {
                        isMagic = true;
                        if (creature.handLeft.caster.isFiring)
                            creature.handLeft.caster.Fire(false);
                        creature.handLeft.caster.UnloadSpell();
                    }
                    else if (part == creature.handRight.lowerArmPart && creature.handRight.caster.spellInstance != null)
                    {
                        isMagic = true;
                        if (creature.handRight.caster.isFiring)
                            creature.handRight.caster.Fire(false);
                        creature.handRight.caster.UnloadSpell();
                    }
                }
            }
            if (isMagic) PlayEffect();
        }

        private void CollisionHandler_OnCollisionStartEvent(CollisionInstance collisionInstance)
        {
            bool isMagic = false;
            if (collisionInstance.damageStruct.damage == 0 || collisionInstance.ignoreDamage)
            {
                if (collisionInstance.sourceCollider?.GetComponentInParent<ItemMagicProjectile>() is ItemMagicProjectile magic)
                {
                    List<EffectDecal> decals = new List<EffectDecal>();
                    collisionInstance.effectInstance.Stop();
                    foreach (Effect effect in collisionInstance.effectInstance.effects)
                    {
                        if (effect.GetType() == typeof(EffectDecal)) decals.Add(effect as EffectDecal);
                        effect.Stop();
                    }
                    for (int i = 0; i < decals.Count; i++)
                    {
                        decals[i].Despawn();
                        decals.RemoveAt(i);
                        i--;
                    }
                    decals.Clear();
                    //magic.item.Despawn();
                    isMagic = true;
                }
                if (collisionInstance.sourceCollider?.GetComponentInParent<Item>() is Item item)
                {
                    if (collisionInstance.sourceColliderGroup?.imbue?.energy > 0f)
                    {
                        collisionInstance.sourceColliderGroup.imbue.Stop();
                        List<EffectDecal> decals = new List<EffectDecal>();
                        foreach (Effect effect in collisionInstance.effectInstance.effects)
                        {
                            if (effect.GetType() == typeof(EffectDecal)) decals.Add(effect as EffectDecal);
                            effect.Stop();
                        }
                        for (int i = 0; i < decals.Count; i++)
                        {
                            decals[i].Despawn();
                            decals.RemoveAt(i);
                            i--;
                        }
                        decals.Clear();
                        isMagic = true;
                    }
                    if (item.lastHandler == null && item.physicBody.velocity.magnitude >= 2)
                    {
                        if (item.GetComponent<ImagineBreakerTKCheck>() == null) item.Despawn();
                        else
                        {
                            item.physicBody.velocity = Vector3.zero;
                            item.physicBody.angularVelocity = Vector3.zero;
                        }
                        isMagic = true;
                    }
                    if (item.isTelekinesisGrabbed)
                    {
                        foreach (Handle handle in collisionInstance.sourceCollider.GetComponentInParent<Item>().handles)
                        {
                            handle.telekinesisHandler?.telekinesis?.TryRelease(false);
                        }
                        item.physicBody.velocity = Vector3.zero;
                        item.physicBody.angularVelocity = Vector3.zero;
                        isMagic = true;
                    }
                }
                if (collisionInstance.sourceColliderGroup == currentSpellCaster.ragdollHand.colliderGroup && collisionInstance.targetColliderGroup?.GetComponentInParent<RagdollHand>() is RagdollHand hand && hand.caster.spellInstance != null && hand.caster.spellInstance.GetType() != typeof(ImagineBreakerSpell))
                {
                    if (!collisionInstance.casterHand.mana.creature.isPlayer && collisionInstance.casterHand.isFiring)
                    {
                        collisionInstance.casterHand.mana.creature.TryPush(Creature.PushType.Magic, -(currentSpellCaster.transform.position - collisionInstance.casterHand.transform.position).normalized, 1);
                    }
                    isMagic = true;
                }
                if (collisionInstance.targetCollider.GetComponentInParent<Creature>() is Creature creature)
                {
                    if (creature.brain.isElectrocuted)
                    {
                        creature.StopShock();
                        isMagic = true;
                    }
                    creature.ragdoll.StopCoroutine("NoGravityCoroutine");
                    foreach (Ragdoll.PhysicModifier modifier in creature.ragdoll.physicModifiers)
                    {
                        if (modifier.handler is SpellCastGravity)
                        {
                            creature.ragdoll.RemovePhysicModifier(modifier.handler);
                            isMagic = true;
                        }
                    }
                }
            }
            if (isMagic) PlayEffect();
        }
        public void PlayEffect()
        {
            instance = Catalog.GetData<EffectData>("ImagineBreakerFx").Spawn(currentSpellCaster.transform, true);
            instance.SetIntensity(1f);
            instance.Play();
        }

        public override void Unload()
        {
            base.Unload();
            currentSpellCaster.ragdollHand.collisionHandler.OnCollisionStartEvent -= CollisionHandler_OnCollisionStartEvent;
            currentSpellCaster.ragdollHand.lowerArmPart.collisionHandler.OnCollisionStartEvent -= CollisionHandler_OnCollisionStartEvent;
            currentSpellCaster.ragdollHand.OnGrabEvent -= RagdollHand_OnGrabEvent;
            currentSpellCaster.ragdollHand.creature.OnDamageEvent -= Creature_OnDamageEvent;
        }
    }
}
