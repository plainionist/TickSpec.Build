namespace TickSpec.Build

type Scenario = {
    Name : string
    Title : string
    Description : string
    Tags : string list
    Body : string list
    StartsAtLine : int
}

type Feature = {
    Name : string
    Filename : string
    Background : string list
    Scenarios : Scenario list
}
