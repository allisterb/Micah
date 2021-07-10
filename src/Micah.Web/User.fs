namespace Micah.Web

open System.Collections.Generic

open WebSharper
open WebSharper.UI
open WebSharper.JavaScript

open WebSharper.JQuery
open Micah.Models
open Micah.NLU

[<JavaScript>]
module User =
    let name = "User"
    let debug m = ClientExtensions.debug name m
    
    /// Update the dialogue state
    let rec update d =        
        Dialogue.debugInterpreterStart d debug name

        let (Dialogue.Dialogue(cui, props, dialogueQuestions, output, utterances)) = d
        
        let echo = Dialogue.echo d
        let say' = Dialogue.say' d
        let say = Dialogue.say d
        let doc = cui.EchoDoc
        let sayRandom = Dialogue.sayRandom d
        let sayRandom' = Dialogue.sayRandom' d

        (* Manage the dialogue state elements*)
        let have = Dialogue.have d 
        let prop k  = Dialogue.prop d k
        let add k v = Dialogue.add d debug k v
        let remove = Dialogue.remove d debug

        let pushu = Dialogue.pushu d debug
        let pushq = Dialogue.pushq d debug
        let popu() = Dialogue.popu d debug
        let popq() = Dialogue.popq d debug
        
        let dispatch = Dialogue.dispatch d debug
        let handle = Dialogue.handle d debug
        let trigger = Dialogue.trigger d debug update
        let cancel = Dialogue.cancel d debug
        let endt = Dialogue.endt d debug
        let didNotUnderstand() = Dialogue.didNotUnderstand d debug name

        let ask = Questions.ask d debug

        (* Base dialogue patterns *)
        let (|Agenda|_|) = Dialogue.(|Agenda_|_|) d
        let (|PropSet|_|) = Dialogue.(|PropSet_|_|) d
        let (|PropNotSet|_|) = Dialogue.(|PropNotSet_|_|) d
        let (|User|_|) = Dialogue.(|User_|_|) d
        let (|User'|_|) = Dialogue.(|User'_|_|) d
        let (|Response|_|) = Dialogue.(|Response_|_|) d
        let (|Response'|_|) = Dialogue.(|Response'_|_|) d
       
        let user():User = prop "user"
        
        (* User functions *)
        let switchUserQuestion u = Question("switchUser", name, Verification ((fun _ -> trigger "verify" "yes"), (fun _ -> trigger "reject" "no")), None, fun _ -> say <| sprintf "Do you want me to switch to the user %s" u)

        let loginUser u = 
            do sayRandom waitRetrievePhrases "user name"
            async { 
                match! Server.getUser u with 
                | Some user ->
                    let setupBox1(b:SweetAlert.Box) =
                        b.Input <- "text"
                        b.ShowCancelButton <- false
                        b.ConfirmButtonText <- "Ok"
                           
                    let collectFaceAndTypingData() =
                        let c = createDialogueBoxCanvas()
                        startCamera JS.Document.Body c
       
                    let rec box(c:int, data: string array array) = 
                        questionBox "Biometric Authentication" "" (Some (boxesWithTitles [|"2"|])) (Some (640, 480)) (Some setupBox1) (Some (collectFaceAndTypingData)) (fun o ->                         
                            let text = o.Value :?> string
                            let image = getCameraCanvas().ToDataURL();
                            debug <|sprintf "User image is %s..." (image.Substring(0, 10))
                            stopCamera()
                            debug <| sprintf "User entered text %s." text
                            sayRandom helloUserPhrases user.Name
                            add "user" u
                            async {
                                do! Server.updateUserLastLogin user.Name |> Async.Ignore
                                if Option.isSome user.LastLoggedIn then 
                                    let! h = Server.humanize user.LastLoggedIn.Value
                                    say <| sprintf "You last logged in %s." h
                            } |> Async.Start
                            doc <| Doc.Concat [
                                Bs.btnPrimary "new" (fun _ _ -> trigger "journal" "journal")
                                Html.text "     "
                                Bs.btnSuccess "query" (fun _ _ -> trigger "symptom_journal" "symptom_journal")
                                Html.text "     "
                                Bs.btnInfo "options" (fun _ _ -> trigger "medication_journal" "medication_journal")
                                Html.text "     "
                                Bs.btnPrimary "help" (fun _ _ -> trigger "help" "help")
                                Bs.btnPrimaryDropdown "dropdownn" ["one";"two"] [(fun _ _ -> trigger "symptom_journal" "symptom_journal"); (fun _ _ -> trigger "symptom_journal" "symptom_journal")]
                            ]
                        )        
                    box(0, [||])

                | None _ -> 
                    say <| sprintf "I did not find a user with the name %s." u
                    Question("addUser", name, Verification ((fun _ -> trigger "verify" "yes"), (fun _ -> trigger "reject" "no")), None, fun _ -> add "addUser" u; say <| sprintf "Do you want me to add the user %s?" u) |> ask
            } |> Async.Start
        
        let addUser u = 
            async { 
                do sayRandom waitAddPhrases "user"
                match! Server.addUser u with 
                | Ok _ -> 
                        add "user" u
                        add "newuser" true
                        say <| sprintf "Hello %A, nice to meet you." props.["user"]
                        say "Click on one of the buttons below to get a list of writing prompts."
                | Error e -> 
                    error <| sprintf "Error adding user %s:%s." u e
                    say <| sprintf "Sorry I was not able to add the user %s to the system." u
            } |> Async.Start
          
        (* Interpreter logic begins here *)
        match Dialogue.frame utterances with
        
        (* User login *)
        | User'(Intent "greet" (_, Entity1Of1 "name" u))::[] -> handle "loginUser" (fun _ -> loginUser u.Value)
        | User'(Intent "hello" (_, Entity1Of1 "contact" u))::[] -> handle "loginUser" (fun _ -> loginUser u.Value)
        
        (* User add *)
        | No(Response' "addUser" (_, _, PStr u))::[] -> endt "addUser" (fun _ -> say <| sprintf "Ok I did not add the user %s. But you must login for me to help you." u)
        | Yes(Response' "addUser" (_, _, PStr u))::[] -> endt "addUser" (fun _ -> addUser u)
        
        (* User switch *)
        | User(Intent "hello" (None, Entity1Of1 "name" u))::[] -> 
            async {
                match! Server.getUser u.Value with
                | Some user -> switchUserQuestion user.Name |> ask
                | None -> say <| sprintf "Sorry, the user %s does not exist." u.Value
            } |> Async.Start
        | Yes(Response "switchUser" (_, _, PStr user))::[] ->
            props.["user"] <- user
            if have "newuser" then remove "newuser"
            say <| sprintf "Ok I switched to user %A." user  
        | No(Response "switchUser" (_, _, PStr user))::[] -> 
            say <| sprintf "Ok I did not switch to user %s." user
        
        | User(Intent "journal" _)::[] 
        | User(Intent "symptom_journal" _)::[]
        | User(Intent "medication_journal" _)::[] -> Journal.update d
        
        | _ -> didNotUnderstand()

        Dialogue.debugInterpreterEnd d debug name
