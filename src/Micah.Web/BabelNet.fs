namespace Micah.Web

open System

open WebSharper
open WebSharper.JQuery

[<JavaScript>]
module BabelNet =

    type DisambiguateApiResponse = {
        tokenFragment: TokenFragment
        charFragment: CharFragment
        babelSynsetID: string
        DBpediaURL: Uri
        BabelNetURL: string
        score: float
        cohrenceScore: float
        globalScore: float
        source: string
    }
    and TokenFragment = {start: int; ``end``:int}
    and CharFragment = {start: int; ``end``:int}

    let disambiguate text :Async<DisambiguateApiResponse list> =
        Async.FromContinuations <| fun (ok, ko, _) -> 
            JQuery.Ajax(
                JQuery.AjaxSettings(
                    Url = sprintf "https://babelfy.io/v1/disambiguate?text=%s&lang=EN&extAIDA=true&key=983fc0ec-a6fa-49ef-bd02-203c18aef272" text,
                    Type = RequestType.GET,
                    Success = Action<obj, string, JqXHR>(fun result s _ -> ok (result :?> DisambiguateApiResponse list)),
                    Error = Action<JqXHR, string, string>(fun jqxhr _ _ -> ko (exn jqxhr.ResponseText))
            )) |> ignore



