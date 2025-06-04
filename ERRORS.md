Assets\Scripts\PlayerStats.cs(633,51): error CS1503: Argument 1: cannot convert from 'HomelessToMillionaire.ShopItem' to 'string'

Assets\Scripts\TimeBasedGameplay.cs(354,17): error CS0152: The switch statement contains multiple cases with the label value '2'

Assets\Scripts\TimeBasedEvents.cs(545,17): error CS0152: The switch statement contains multiple cases with the label value '2'

Assets\Scripts\TimeBasedGameplay.cs(636,17): error CS0152: The switch statement contains multiple cases with the label value '2'

Assets\Scripts\TimeUI.cs(628,17): error CS0152: The switch statement contains multiple cases with the label value '2'

Assets\Scripts\TimeUI.cs(662,17): error CS0152: The switch statement contains multiple cases with the label value '2'

Assets\Scripts\TimeUI.cs(685,17): error CS0152: The switch statement contains multiple cases with the label value '2'

Assets\Scripts\TimeOfDayManager.cs(439,29): error CS0266: Cannot implicitly convert type 'HomelessToMillionaire.TimeSpeed' to 'float'. An explicit conversion exists (are you missing a cast?)

Assets\Scripts\TimeOfDayManager.cs(452,32): error CS0266: Cannot implicitly convert type 'float' to 'HomelessToMillionaire.TimeSpeed'. An explicit conversion exists (are you missing a cast?)

Assets\Scripts\TimeUI.cs(778,21): error CS0152: The switch statement contains multiple cases with the label value '2'

Assets\Scripts\ProgressionUI.cs(1198,36): error CS1503: Argument 1: cannot convert from 'string' to 'HomelessToMillionaire.ShopItem'

Assets\Scripts\ProgressionUI.cs(1209,36): error CS1503: Argument 1: cannot convert from 'HomelessToMillionaire.JobType' to 'HomelessToMillionaire.Job'

Assets\Scripts\ProgressionUI.cs(1231,45): error CS1503: Argument 1: cannot convert from 'HomelessToMillionaire.EducationType' to 'HomelessToMillionaire.EducationCourse'

private TimeBasedModifiers GetModifiersForTimePeriod(TimePeriod period)
{
    switch (period)
    {
        case TimePeriod.EarlyMorning:
        case TimePeriod.Morning:
            return morningModifiers;
        case TimePeriod.Day:
        case TimePeriod.Afternoon:
            return dayModifiers;
        case TimePeriod.Evening:
            return eveningModifiers;
        case TimePeriod.Night:
        case TimePeriod.LateNight:
            return nightModifiers;
        default:
            return dayModifiers;
    }
}
