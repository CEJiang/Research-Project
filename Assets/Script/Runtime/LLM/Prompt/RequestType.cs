public enum RequestType
{
    None,
    WhatWillYouDo,         // Action Planning
    WhatDoYouThink,        // Observation Thought
    WhyDidYouDoThat,       // Decision Reason
    WhatMightHaveHappened, // Past Inference
    WhatWillHappenNext,    // Future Inference/Prediction
    WhatDoYouTellYourself  // Self Reflection
}

public static class RequestTypeExtensions
{
    public static string ToPromptText(this RequestType type)
    {
        return type switch
        {
            RequestType.WhatWillYouDo         => "Tell what you do next",
            RequestType.WhatDoYouThink        => "Tell what you think",
            RequestType.WhyDidYouDoThat       => "Tell the reason for this decision",
            RequestType.WhatMightHaveHappened => "Speculate what happened here",
            RequestType.WhatWillHappenNext    => "Speculate what will happen next",
            RequestType.WhatDoYouTellYourself => "Tell what you say to yourself",
            _ => string.Empty,
        };
    }
}
