using System;
using System.Collections.Generic;
using System.Linq;

public static class TooltipManager
{
    private static List<Tooltip> activeTooltips = new List<Tooltip>();

    public static event Action<Tooltip> OnActiveTooltipChanged;


    public static bool HasTooltip(Tooltip tooltip) => activeTooltips.Contains(tooltip);


    public static void AddTooltip(Tooltip newTooltip)
    {
        if(activeTooltips.Contains(newTooltip))
        {
            //Don't add duplicate tooltips
            return;
        }

        activeTooltips.Add(newTooltip);
        OnActiveTooltipChanged?.Invoke(newTooltip);
    }


    public static void RemoveTooltip(Tooltip remove)
    {
        if(activeTooltips.Contains(remove))
        {
            activeTooltips.Remove(remove);
        }

        //Update the tooltip with the last one that was added, or null
        Tooltip newActive = activeTooltips.Count > 0 ? activeTooltips.Last() : null;
        OnActiveTooltipChanged?.Invoke(newActive);
    }
}