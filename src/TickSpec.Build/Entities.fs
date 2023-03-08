namespace TickSpec.Build

type Scenario = {
    Name : string
    Title : string
    StartsAtLine : int
}

type Feature = {
    Name : string
    Filename : string
    Scenarios : Scenario list
}
