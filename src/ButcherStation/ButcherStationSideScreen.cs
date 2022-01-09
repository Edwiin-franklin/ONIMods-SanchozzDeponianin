﻿using System;
using UnityEngine;
using SanchozzONIMods.Lib.UI;
using PeterHan.PLib.UI;

namespace ButcherStation
{
    internal class ButcherStationSideScreen : SideScreenContent
    {
        private const string prefix = "STRINGS.UI.UISIDESCREENS.BUTCHERSTATIONSIDESCREEN.";
        private ButcherStation target;
        // каллбаки для чекбоксов и слидеров
        private Action<bool> wrangle_unselected;
        private Action<bool> wrangle_old_aged;
        private Action<bool> wrangle_surplus;
        private Action<bool> leave_alive;
        private Action<bool> enable_leave_alive;
        private Action<float> age_threshold;
        private Action<float> creature_limit;
        protected override void OnPrefabInit()
        {
            var margin = new RectOffset(6, 6, 6, 6);
            var baseLayout = gameObject.GetComponent<BoxLayoutGroup>();
            if (baseLayout != null)
                baseLayout.Params = new BoxLayoutParams()
                {
                    Alignment = TextAnchor.MiddleLeft,
                    Margin = margin,
                };
            var panel = new PPanel("MainPanel")
            {
                Alignment = TextAnchor.MiddleLeft,
                Direction = PanelDirection.Vertical,
                Margin = margin,
                Spacing = 8,
                FlexSize = Vector2.right,
            }
                .AddCheckBox(prefix, nameof(wrangle_unselected),
                    b => { if (target != null) target.wrangleUnSelected = b; }, out wrangle_unselected, out _)
                .AddCheckBox(prefix, nameof(wrangle_old_aged),
                    b => { if (target != null) target.wrangleOldAged = b; }, out wrangle_old_aged, out _)
                .AddSliderBox(prefix, nameof(age_threshold), 0f, 100f,
                    f => { if (target != null) target.ageButchThresold = f / 100f; }, out age_threshold)
                .AddCheckBox(prefix, nameof(wrangle_surplus),
                    b => { if (target != null) target.wrangleSurplus = b; }, out wrangle_surplus, out _)
                .AddSliderBox(prefix, nameof(creature_limit), 0f, ButcherStationOptions.Instance.max_creature_limit,
                    f => { if (target != null) target.creatureLimit = Mathf.RoundToInt(f); }, out creature_limit)
                .AddCheckBox(prefix, nameof(leave_alive),
                    b => { if (target != null) target.leaveAlive = b; }, out leave_alive, out enable_leave_alive)
                .AddChild(new PLabel("Bottom")
                {
                    Text = Strings.Get(prefix + "FILTER_LABEL"),
                    TextStyle = PUITuning.Fonts.TextDarkStyle
                })
                .AddTo(gameObject);
            ContentContainer = gameObject;
            titleKey = prefix + "TITLE";
            base.OnPrefabInit();
            UpdateScreen();
        }

        private void UpdateScreen()
        {
            if (target != null)
            {
                wrangle_unselected?.Invoke(target.wrangleUnSelected);
                wrangle_old_aged?.Invoke(target.wrangleOldAged);
                wrangle_surplus?.Invoke(target.wrangleSurplus);
                leave_alive?.Invoke(target.leaveAlive);
                enable_leave_alive?.Invoke(target.allowLeaveAlive);
                age_threshold?.Invoke(target.ageButchThresold * 100f);
                creature_limit?.Invoke(target.creatureLimit);
            }
        }

        public override bool IsValidForTarget(GameObject target) => target.GetComponent<ButcherStation>() != null;

        public override void SetTarget(GameObject target)
        {
            this.target = target?.GetComponent<ButcherStation>();
            UpdateScreen();
        }

        public override void ClearTarget() => target = null;

        public override int GetSideScreenSortOrder() => 30;
    }
}
