﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.Replays;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Catch.Mods
{
    public class CatchModCinema : ModCinema<CatchHitObject>
    {
        public override Type[] IncompatibleMods => base.IncompatibleMods.Concat(new[] { typeof(CatchModMovingFast) }).ToArray();

        public override ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => new ModReplayData(new CatchAutoGenerator(beatmap).Generate(), new ModCreatedUser { Username = "osu!salad" });
    }
}
