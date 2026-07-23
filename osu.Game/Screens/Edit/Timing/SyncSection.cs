// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.Edit.Timing
{
    internal partial class SyncSection : CompositeDrawable
    {
        private readonly BindableBool syncBookmarks = new BindableBool(true);
        private readonly BindableBool syncPreviewPoint = new BindableBool(true);

        private RoundedButton syncButton = null!;

        private const float header_height = 50;

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [Resolved]
        private Editor? editor { get; set; }

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private IBindable<WorkingBeatmap> working { get; set; } = null!;

        [Resolved]
        private IDialogOverlay? dialogOverlay { get; set; }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colours)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Masking = true;
            CornerRadius = 5;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = colours.Background4,
                    RelativeSizeAxes = Axes.Both,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = header_height,
                    Padding = new MarginPadding { Horizontal = 10 },
                    Child = new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = EditorStrings.Sync,
                    }
                },
                new Container
                {
                    Y = header_height,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Child = new FillFlowContainer
                    {
                        Padding = new MarginPadding(10) { Top = 0 },
                        Spacing = new Vector2(10),
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            new FormCheckBox
                            {
                                Caption = EditorDialogsStrings.SyncTimingOptionBookmarks,
                                Current = { BindTarget = syncBookmarks },
                            },
                            new FormCheckBox
                            {
                                Caption = EditorDialogsStrings.SyncTimingOptionPreviewPoint,
                                Current = { BindTarget = syncPreviewPoint },
                            },
                            syncButton = new RoundedButton
                            {
                                RelativeSizeAxes = Axes.X,
                                Text = EditorStrings.SyncToAllDifficulties,
                                TooltipText = EditorStrings.SyncToAllDifficultiesTooltip,
                                Action = () => dialogOverlay?.Push(new SyncTimingConfirmationDialog(syncToAllOtherDifficulties)),
                            }
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            syncBookmarks.BindValueChanged(_ => updateEnabledState());
            syncPreviewPoint.BindValueChanged(_ => updateEnabledState());
            updateEnabledState();
        }

        private void updateEnabledState()
        {
            bool canSyncToOtherDifficulties = working.Value.BeatmapSetInfo.Beatmaps.Count > 1;

            syncBookmarks.Disabled = !canSyncToOtherDifficulties;
            syncPreviewPoint.Disabled = !canSyncToOtherDifficulties;

            syncButton.Enabled.Value = canSyncToOtherDifficulties
                                       && (syncBookmarks.Value || syncPreviewPoint.Value);
        }

        private void syncToAllOtherDifficulties()
        {
            if (working.Value.BeatmapSetInfo.Beatmaps.Count <= 1)
                return;

            if (!syncBookmarks.Value && !syncPreviewPoint.Value)
                return;

            var set = working.Value.BeatmapSetInfo;
            var current = beatmap.BeatmapInfo;
            int[] sourceBookmarks = beatmap.Bookmarks.ToArray();
            int sourcePreviewTime = beatmap.BeatmapInfo.Metadata.PreviewTime;

            foreach (var beatmapInfo in set.Beatmaps)
            {
                if (beatmapInfo.Equals(current))
                    continue;

                try
                {
                    var targetWorking = beatmaps.GetWorkingBeatmap(beatmapInfo);
                    var playable = targetWorking.GetPlayableBeatmap(beatmapInfo.Ruleset);

                    if (syncBookmarks.Value)
                        playable.Bookmarks = sourceBookmarks.ToArray();

                    if (syncPreviewPoint.Value)
                        beatmapInfo.Metadata.PreviewTime = sourcePreviewTime;

                    beatmaps.Save(beatmapInfo, playable, targetWorking.GetSkin(), targetWorking.Storyboard);
                }
                catch (Exception e)
                {
                    Logger.Error(e, $@"Failed to sync bookmarks/preview to {beatmapInfo.GetDisplayTitle()}");
                    return;
                }
            }

            editor?.SaveAndReload(withDialog: false);
        }
    }
}
