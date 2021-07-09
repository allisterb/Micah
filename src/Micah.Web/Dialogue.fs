namespace Micah.Web

open System.Collections.Generic

open WebSharper
open WebSharper.JavaScript
open WebSharper.JQuery
open Micah.Web

[<JavaScript>]
type Dialogue = Dialogue of CUI * Dictionary<string, obj> * Stack<Question> * Stack<string> * Stack<Utterance> with
    member x.Cui = let (Dialogue(c,_, _, _, _)) = x in c 
    member x.Props = let (Dialogue(_,p, _, _, _)) = x in p
    member x.DialogueQuestions = let (Dialogue(_, _, dq, _, _)) = x in dq
    member x.Output = let (Dialogue(_, _, _, o, _)) = x in o
    member x.Utterances = let (Dialogue(_, _, _, _, u)) = x in u
and 
    [<JavaScript>] Question = Question of string * string * QuestionType  * obj[] option * (Dialogue->unit) with 
        member x.Name = let (Question(n, _, _, _,_)) = x in n 
        member x.Module = let (Question(_, m, _, _,_)) = x in m
        member x.Type = let (Question(_, _, ty, _, _)) = x in ty
        member x.Params = let (Question(_, _, _, p, _)) = x in p
        member x.Target = let (Question(_, _, _, _, a)) = x in a
        override x.ToString() = sprintf "Name: %s Module: %s Type: %A " x.Name x.Module x.Type
and 
    [<JavaScript>] QuestionType =
    | UserAuthentication of string
    | Verification of (unit->unit) * (unit->unit)
    | WritingPrompt of string list * (int->unit)
    | Disjunctive 
    | ConceptCompletion 

[<JavaScript>]
type Form = Form of string * Question list

[<JavaScript>]
module Dialogue =
    let echo (d:Dialogue) t = d.Cui.EchoHtml' t

    let say' (d:Dialogue) (t:string) = d.Cui.Say t
 
    let say (d:Dialogue) t =
        d.Output.Push t
        say' d t

    let sayRandom (d:Dialogue) p v  = 
        let t = getRandomPhrase p v
        d.Output.Push(t) |> ignore
        d.Cui.Say t
 
    let sayRandom' (d:Dialogue) p = sayRandom d p ""
 
    (* Manage the dialogue state elements*)
    let have (d:Dialogue) k = d.Props.ContainsKey k
    let prop<'a> (d:Dialogue) k :'a = if d.Props.ContainsKey k then d.Props.[k] :?> 'a else failwithf "The %s dialogue property does not exist." k
    let add<'a> (d:Dialogue) (debug:string -> unit) k (v:'a) = 
        d.Props.Add(k, v)
        debug <| sprintf "Add property %s:%A." k v
    
    let remove (d:Dialogue) (debug:string -> unit) k = 
        debug <| sprintf "Remove property %s." k
        d.Props.Remove k |> ignore
 
    let pushu (d:Dialogue) (debug:string -> unit) (m:Utterance) = 
        debug <| sprintf "Push %A." m
        d.Utterances.Push m 

    let popu (d:Dialogue) (debug:string -> unit) = 
        let m = d.Utterances.Pop()
        debug <| sprintf "Pop %A." m
     
    let pushq (d:Dialogue) (debug:string -> unit) (q:Question) = 
        d.DialogueQuestions.Push q
        debug <| sprintf "Push %A." q

    let popq(d: Dialogue) (debug:string -> unit) = 
        let q = d.DialogueQuestions.Pop()
        debug <| sprintf "Pop %A." q
        
    let dispatch (d:Dialogue) (debug:string -> unit) (targetModule:string) (target:Dialogue->unit) =
        debug <| sprintf "Dispatch to module %s utterances: %A questions: %A." targetModule d.Utterances d.DialogueQuestions
        target d

    let handle (d:Dialogue) (debug:string -> unit) (m:string) (f:unit->unit) =
        popu d debug
        debug <| sprintf "Handle utterance %s." m
        f()
        
    let trigger<'a> (d:Dialogue) (debug:string -> unit) (target:Dialogue->unit) (name:string) (data:'a)=
        pushu d debug (Utterance(Json.Stringify data, Some(Intent(name, Some 1.0f)), None, None))
        target d

    let cancel (d:Dialogue) (debug:string -> unit) (qn:string) =
        let q = d.DialogueQuestions.Peek()
        if q.Name <> qn then failwithf "%A at the top of the stack does not have the name %s." q qn
        popq d debug 
        debug <| sprintf "Cancel %A." q 

    let endt (d:Dialogue) (debug:string -> unit) (m:string) (f:unit->unit) =
        popu d debug
        popq d debug
        if have d m then remove d debug m
        debug <| sprintf "End turn %s." m
        f()
       
    let didNotUnderstand (d:Dialogue) (debug:string -> unit) (name:string) =
        debug <| sprintf "%s interpreter did not understand utterance." name
        popu d debug
        say d "Sorry I didn't understand what you meant."
        if not (have d "user") then say d "You must login to use most functions in Selma."
      
    let frame (utterances:Stack<Utterance>) = utterances |> Seq.take (if utterances.Count >= 5 then 5 else utterances.Count) |> List.ofSeq
    
    let debugInterpreterStart (d:Dialogue) (debug:string -> unit) (name:string) =
        debug <| sprintf "%s starting utterances:%A, questions: %A." name d.Utterances d.DialogueQuestions
    
    let debugInterpreterEnd (d:Dialogue) (debug:string -> unit) (name:string) =
        debug <| sprintf "%s ending utterances:%A, questions: %A." name d.Utterances d.DialogueQuestions
    
    (* Dialogue patterns *)
    let (|Agenda_|_|) (d:Dialogue) (debug:string -> unit) (m:string) :Utterance list -> unit option =
        function
        | _ when d.DialogueQuestions.Count > 0 && d.DialogueQuestions.Peek().Module = m -> Some ()
        | _ -> None

    let (|PropSet_|_|) (d:Dialogue) (n:string) :Utterance -> Utterance option =
         function
         | m when have d n -> Some m
         | _ -> None

    let (|PropNotSet_|_|) (d:Dialogue) (n:string) :Utterance -> Utterance option =
         function
         | m when not (have d n) -> Some m
         | _ -> None
  
    let (|User_|_|) (d:Dialogue) :Utterance -> Utterance option =
         function
         | PropSet_ d "user" m when d.DialogueQuestions.Count = 0 -> Some m
         | _ -> None

    let (|User'_|_|) (d:Dialogue) :Utterance -> Utterance option =
         function
         | PropNotSet_ d "user" m when d.DialogueQuestions.Count = 0 -> Some m
         | _ -> None

    let (|Form|_|) (d:Dialogue) (n:string) :Utterance -> Utterance option =
         function
         | m when d.DialogueQuestions.Count > 0 && d.DialogueQuestions.Peek().Name = n && m.Intent.IsSome && m.Intent.Value.Name = n && m.Intent.Value.Confidence = Some(1.0f) && m.Traits = None && m.Entities = None -> Some m
         | _ -> None

    let (|NotForm|_|) (d:Dialogue) (n:string) :Utterance -> Utterance option =
         function
         | m when d.DialogueQuestions.Count > 0 && d.DialogueQuestions.Peek().Name = n && m.Text <> "" -> Some m
         | _ -> None

    let (|Response_|_|) (d:Dialogue) (n:string) :Utterance -> (Utterance * string option * obj option) option =
         function
         | PropSet_ d "user" (Form d n m) 
         | PropSet_ d "user" (NotForm d n m) -> 
            let p = if have d n then Some(prop d n) else None 
            if m.Text <> "" then Some (m, Some(m.Text), p) else Some(m, None, p)
         | _ -> None

    let (|Response'_|_|) (d:Dialogue) (n:string) :Utterance -> (Utterance * string option * obj option) option =
         function
         | PropNotSet_ d "user" (Form d n m) 
         | PropNotSet_ d "user" (NotForm d n m) -> 
            let p = if have d n then Some(prop d n) else None
            if m.Text <> "" then Some (m, Some(m.Text), p) else Some(m, None, p)
         | _ -> None
