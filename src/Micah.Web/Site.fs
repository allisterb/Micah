namespace Micah.Web

open WebSharper
open WebSharper.JavaScript
open WebSharper.Web

open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Server

type Route =
    | [<EndPoint"/">] Home

module Templates =
    type MainTemplate = Templating.Template<"wwwroot/Main.html">

    let Main ctx action (title: string) (body: Doc list) =
        Content.Page (
            MainTemplate()
                .Title(title)
                .Body(body)
                .Doc()
        )
    
module Site =
    let HomePage ctx =
        Templates.Main ctx Home "Selma" [
            div [attr.id "term"; attr.``class`` "container"] [
                client <@ Client.run() @>                
            ]
        ]
            
    [<Website>]
    let Main = 
        Sitelet.Infer <| fun ctx route ->
            match route with
            | Home -> HomePage ctx
