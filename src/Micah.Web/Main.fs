namespace Micah.Web

open System.Collections.Generic

open WebSharper
open WebSharper.UI

[<JavaScript>]
module Main =
    let name = "Main"
    let debug m = ClientExtensions.debug name m
           
    /// Update the dialogue state
    let rec update d =        
        let (Dialogue.Dialogue(cui, props, dialogueQuestions, output, utterances)) = d
        debug <| sprintf "Module %s starting utterances:%A, questions: %A." name utterances dialogueQuestions
   
        let echo = Dialogue.echo d
        let say' = Dialogue.say' d
        let say = Dialogue.say d
        let doc = cui.EchoDoc
        let sayRandom = Dialogue.sayRandom d
        let sayRandom' = Dialogue.sayRandom' d

        (* Manage the dialogue state elements t*)
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
        let endt = Dialogue.endt d debug
        let didNotUnderstand() = Dialogue.didNotUnderstand d debug name

        let ask = Questions.ask d debug

        let showMainButtons() =
            doc <| Doc.Concat [
                Bs.btnPrimary "journal" (fun _ _ -> trigger "journal" "journal")
                Html.text "     "
                Bs.btnSuccess "symptoms" (fun _ _ -> trigger "symptoms" "symptoms")
                Html.text "     "
                Bs.btnInfo "medication" (fun _ _ -> trigger "medication" "medication")
                Html.text "     "
                Bs.btnPrimary "help" (fun _ _ -> trigger "help" "help")
            ]

        (* Base dialogue patterns *)
        let (|Agenda|_|) = Dialogue.(|Agenda_|_|) d debug
        let (|PropSet|_|) = Dialogue.(|PropSet_|_|) d
        let (|PropNotSet|_|) = Dialogue.(|PropNotSet_|_|) d
        let (|User|_|) = Dialogue.(|User_|_|) d
        let (|User'|_|) = Dialogue.(|User'_|_|) d
        let (|Response|_|) = Dialogue.(|Response_|_|) d
        let (|Response'|_|) = Dialogue.(|Response'_|_|) d
        
        (* Module dialogue patterns *) 
        let (|Start|_|) =
            function
            | PropNotSet "started" m -> Some m
            | _ -> None
   
        (* Interpreter logic begins here *)
        match Dialogue.frame utterances with
        
        (* Agenda *)
        | Agenda User.name -> 
            debug <| sprintf "Agenda is %A." (d.DialogueQuestions.Peek())
            User.update d

        (* Help *)
        | Intent "help" _::[] ->
            say "The following commands are available."
            echo "The following commands are available:"
            echo "<span style='background-color:blue;color:white'>journal</span> - Show a list of writing prompts"
            echo "<span style='background-color:blue;color:white'>debug-journal</span> <text-entry> - Show a set of debug info for a journal entry."
        
        
        (* Greet *)
        | Start(User'(Intent "greet" (_, None)))::[] ->  
                add "started" true
                handle "greet" (fun _ -> sayRandom' helloPhrases)

        | User'(Intent "greet" (_, None))::[] -> handle "greet" (fun _ -> say "Hello, tell me your name to get started.")
          
        (* Dispatch *)
        
        (* User login *)
        | User'(Intent "greet" (_, Entity1Of1 "name" _))::[] -> dispatch User.name User.update
        | User'(Intent "hello" (_, Entity1Of1 "contact" _))::[] -> dispatch User.name User.update
        | User'(Intent "greet" (_, Entity1OfAny "name" u))::[]
        | User'(Intent "greet" (_, Entity1OfAny "contact" u))::[]-> dispatch User.name User.update
       

        (* Journal entry *)
        | User(Intent "journal" _)::[] -> dispatch Journal.name Journal.update

        | _ -> didNotUnderstand()

        Dialogue.debugInterpreterEnd d debug name
