// <copyright file="AxeComboOrbwalkingMode.cs" company="Ensage">
//    Copyright (c) 2017 Ensage.
// </copyright>

namespace Vaper.OrbwalkingModes
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Ensage.Common.Extensions;
    using Ensage.SDK.Orbwalker.Modes;

    using SharpDX;

    using Vaper.Heroes;

    public class AxeComboOrbwalkingMode : KeyPressOrbwalkingModeAsync
    {
        private readonly Axe hero;

        public AxeComboOrbwalkingMode(Axe hero)
            : base(hero.Ensage.Orbwalker, hero.Ensage.Input, hero.Menu.General.ComboKey)
        {
            this.hero = hero;
        }

        public override async Task ExecuteAsync(CancellationToken token)
        {
            if (!this.hero.Owner.IsAlive || this.hero.IsKillstealing)
            {
                await Task.Delay(125, token);
                return;
            }

            var blink = this.hero.Blink;
            var maxRange = blink?.CastRange * 1.5f ?? 1000.0f;

            var target = this.hero.Ensage.TargetSelector.Active.GetTargets().FirstOrDefault(x => x.Distance2D(this.Owner) <= maxRange);
            if (target == null)
            {
                this.hero.Ensage.Orbwalker.Active.OrbwalkTo(null);
                return;
            }

            var cullingBlade = this.hero.CullingBlade;
            var cullingBladeKill = cullingBlade.CanBeCasted && cullingBlade.GetDamage(target) > target.Health && !target.IsLinkensProtected();

            var call = this.hero.Call;

            if (blink != null && blink.CanBeCasted && blink.CanHit(target))
            {
                if ((call.CanBeCasted && !call.CanHit(target)) 
                    || (cullingBladeKill && !cullingBlade.CanHit(target)))
                {
                    // TODO: get best blink location with prediction to hit target + max other targets
                    var blinkPos = target.IsMoving ? target.InFront(75) : target.Position;
                    blink.UseAbility(blinkPos);
                    await Task.Delay(blink.GetCastDelay(blinkPos), token);
                }
            }

            if (cullingBladeKill && cullingBlade.CanHit(target))
            {
                cullingBlade.UseAbility(target);
                await Task.Delay(cullingBlade.GetCastDelay(target), token);
            }
            else
            {
                if (call.CanBeCasted && call.CanHit(target))
                {
                    var bladeMail = this.hero.BladeMail;
                    if (bladeMail != null && bladeMail.CanBeCasted)
                    {
                        bladeMail.UseAbility();
                        await Task.Delay(bladeMail.GetCastDelay(), token);
                    }

                    var lotusOrb = this.hero.LotusOrb;
                    if (lotusOrb != null && lotusOrb.CanBeCasted)
                    {
                        lotusOrb.UseAbility(this.Owner);
                        await Task.Delay(lotusOrb.GetCastDelay(), token);
                    }

                    call.UseAbility();
                    await Task.Delay(call.GetCastDelay(), token);
                }
            }

            this.hero.Ensage.Orbwalker.Active.OrbwalkTo(target);
        }
    }
}