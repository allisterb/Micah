namespace Micah.Web

open System
open System.Collections.Generic;
open System.Linq

open FSharp.Control

open WebSharper
open Npgsql.FSharp
open Humanizer
open Newtonsoft.Json

open Micah
open Micah.Models
open Micah.NLU.ExpertAI
open Micah.NLU.GoogleHC
open Micah.FHIR

module Server =        
    
    let private pgdb =
        Sql.host (Runtime.Config("PGSQL"))
        |> Sql.port 5432
        |> Sql.username "micah"
        |> Sql.password "micah"
        |> Sql.database "micah"
        |> Sql.sslMode SslMode.Prefer
        |> Sql.config "Pooling=true"
        |> Sql.formatConnectionString
        |> Sql.connect
 
    (*Config functions *)
    let private getServerConfigVal(name:string) : string  = 
        pgdb
        |> Sql.query "SELECT * FROM config WHERE name=@n"
        |> Sql.parameters ["n", Sql.string name]
        |> Sql.execute (fun read -> read.string "value") 
        |> function 
            | Ok v  -> infof "Retrieved config value {0}={1} from database." [name;v.Head]; v.Head 
            | Error exn -> errex "Error retrieving config value {0} from database." exn [name]; raise exn

    let private expertai = ExpertAIApi(getServerConfigVal("expertai_auth_token"))

    (* Text transformation functions *)
    [<Rpc>]
    let humanize(date:DateTime) = async { return date.Humanize() }

    [<Rpc>]
    let mdtohtml(s:string) = async { return Markdig.Markdown.ToHtml s }

    [<Rpc>]
    let mdtotext(s:string) = async { return Markdig.Markdown.ToPlainText s }

    (* Authentication functions *)
    [<Rpc>]
    let addUserTypingPattern (id:string) (tp: string) = async { return! TypingDNA.savePattern id tp }

    [<Rpc>]
    let verifyUserTypingPattern (id:string) (tp: string) = async { return! TypingDNA.verifyPattern id tp }

    [<Rpc>]
    let enrollUser (userName:string) (voiceData:JavaScript.Int16Array) =
        async {
            match! AzureSpeech.createVoiceProfile() with
            | Error e -> return None
            | Ok profile -> 
                match! AzureSpeech.enrollVoiceProfile profile voiceData with
                | Error e -> 
                    errf "Azure Speech returned {0} when enrolling user {1}." [e; userName]
                    return None
                | Ok r -> 
                    infof "Azure Speech audio profile enrollment for user {0} has: length {1}, speech length {2}, remaining enrollments {3}" [userName; r.AudioLength; r.AudioSpeechLength; r.RemainingEnrollmentsCount]
                    return Some(r)
        }

    [<Rpc>]
    let detectFace (dataUrl:string) = AzureFace.detectFace <| AzureFace.getImageFromDataUrl dataUrl
        
    [<Rpc>]
    let hasFace (dataUrl:string) = 
        async {
            let! f = detectFace dataUrl
            return isVal f
        }

    [<Rpc>]
    let detectFaceAttributes (dataUrl:string) = AzureFace.detectFaceAttributes <| AzureFace.getImageFromDataUrl dataUrl

    [<Rpc>]
    let authUserFace(imageDataUrl:string) =
        async {
            match! detectFace imageDataUrl with
            | None -> return false
            | Some f -> 
                infof "{0}" [f.FaceAttributes.Age.Value.ToString()]
                return true
        }

    [<Rpc>]
    let enrollUserFace(un: string, imgDataUrl:string) = 
        async {
            let uid = Guid.NewGuid().ToString()
            match! AzureFace.addPerson uid with
            | Ok p -> 
                match! imgDataUrl |> AzureFace.getImageFromDataUrl |> AzureFace.enrollPersonFace p with
                | Ok _ -> return Ok uid
                | Error e -> return Error e 
            | Error e -> return Error e
        }

    (* User functions *)
    [<Rpc>]
    let getUser(user:string) : Async<User option> = 
        pgdb
        |> Sql.query "SELECT * FROM micah_user WHERE user_name=@u"
        |> Sql.parameters ["u", Sql.string user]
        |> Sql.executeAsync (fun read -> {
            Name =  read.string("user_name")
            LastLoggedIn = read.timestampOrNone "last_logged_in" |> Option.map(fun t -> t.ToDateTime())
        }) 
        |> Async.map(
            function 
            | Ok u  -> (if u.Length > 0 then infof "Retrieved user {0} from database." [u.Head.Name]; Some u.Head else None) 
            | Error exn -> errex "Error retrieving user {0} to database." exn [user]; None)

    [<Rpc>]
    let addUser (user:string) : Async<Result<unit, string>> =
        pgdb
        |> Sql.query "INSERT INTO public.micah_user(user_name, last_logged_in) VALUES (@u, @d);"
        |> Sql.parameters [("u", Sql.string user); ("d", Sql.timestamp (DateTime.Now))]
        |> Sql.executeNonQueryAsync
        |> Async.map(
            function 
            | Ok n -> if n > 0 then Ok(infof "Added user {0} to database." [user]) else Error("Insert user returned 0.")
            | Error exn as e -> errex "Error adding user {0} to database." exn [user]; Error exn.Message
        )

    [<Rpc>]
    let updateUserLastLogin (user:string) : Async<Result<unit, exn>> =
        pgdb
        |> Sql.query "UPDATE public.micah_user SET last_logged_in=@d WHERE user_name=@u;"
        |> Sql.parameters [("u", Sql.string user); ("d", Sql.timestamp (DateTime.Now))]
        |> Sql.executeNonQueryAsync
        |> Async.map (
            function 
            | Ok n -> if n > 0 then Ok(infof "Updated user {0} last login time in database." [user]) else Error(exn("Insert user returned 0.")) 
            | Error exn -> errex "Error updating user {0} last login time in database." exn [user]; Error exn
        )

    [<Rpc>]
    let updateUserTypingProfileId (user:string) (tid:string) : Async<Result<unit, exn>> =
        pgdb
        |> Sql.query "UPDATE public.micah_user SET typig_logge_in=@d WHERE user_name=@u;"
        |> Sql.parameters [("u", Sql.string user); ("d", Sql.timestamp (DateTime.Now))]
        |> Sql.executeNonQueryAsync
        |> Async.map (
            function 
            | Ok n -> if n > 0 then Ok(infof "Updated user {0} last login time in database." [user]) else Error(exn("Insert user returned 0.")) 
            | Error exn -> errex "Error updating user {0} last login time in database." exn [user]; Error exn
        )

    (* Symptom journal functions *)
    [<Rpc>]
    let addSymptomJournalEntry (user:string) (name:string) (location:string option) (magnitude:int option) :Async<Result<unit, exn>> =
        pgdb
        |> Sql.query "INSERT INTO public.physical_symptom_journal(user_name, name, date, magnitude, location) VALUES (@u, @n, @d, @m, @l);"
        |> Sql.parameters [
            "u", Sql.string user
            "n", Sql.string name
            "d", Sql.timestamp (DateTime.Now) 
            "m", if magnitude.IsSome then Sql.int(magnitude.Value) else Sql.dbnull
            "l", if location.IsSome then Sql.string (location.Value) else Sql.dbnull
        ]
        |> Sql.executeNonQueryAsync
        |> Async.map(
            function 
            | Ok n -> if n > 0 then Ok(infof "Added symptom {0} for user {1} to database." [name;user]) else Error(exn("Insert into symptom journal entry affected 0 rows."))
            | Error exn -> errex "Did not add symptom {0} for user {1} to database" exn [name;user]; Error exn
        )
    
    [<Rpc>]
    let getWritingJournal(userName:string) : Async<WritingJournlEntry list option> = 
        pgdb
        |> Sql.query "SELECT * FROM public.writing_journal WHERE user_name=@u"
        |> Sql.parameters ["u", Sql.string userName]
        |> Sql.executeAsync (fun read -> {
            UserName =  read.string("user_name")
            Date = (read.timestamp "date").ToDateTime() 
            WritingPrompt = (read.int "writing_prompt")
            Text = read.string "text"
            KnowledgeTriples = Newtonsoft.Json.JsonConvert.DeserializeObject<Triple list list>(read.string "knowledge_triples")
            KnowledgeLemmas = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpertAILemma list>(read.string "knowledge_lemmas")
            KnowledgeEntities = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpertAIEntity list>(read.string "knowledge_entities")
            KnowledgeEmotionalTraits = Newtonsoft.Json.JsonConvert.DeserializeObject<EmotionalTrait list>(read.string "knowledge_emotional_traits")
            KnowledgeBehaviouralTraits = Newtonsoft.Json.JsonConvert.DeserializeObject<BehavioralTrait list>(read.string "knowledge_behavioural_traits")
        }) 
        |> Async.map(function | Ok j  -> Some j | Error exn -> err(exn.Message); None)

    [<Rpc>]
    let addWritingJournalEntry (entry:WritingJournlEntry) : Async<Result<unit, string>> = 
        pgdb
        |> Sql.query "INSERT INTO public.writing_journal(user_name, date, writing_prompt, text, knowledge_triples, knowledge_lemmas, knowledge_entities, knowledge_emotional_traits, knowledge_behavioural_traits) VALUES(@u, @d, @w, @t, @kt, @kl, @ke, @ket, @kbt);"
        |> Sql.parameters [
            ("u", Sql.string entry.UserName) 
            ("d", Sql.timestamp DateTime.Now)
            ("w", Sql.int entry.WritingPrompt)
            ("t", Sql.string entry.Text)
            ("kt", Sql.string (JsonConvert.SerializeObject(entry.KnowledgeTriples)))
            ("kl", Sql.string (JsonConvert.SerializeObject(entry.KnowledgeTriples)))
            ("ke", Sql.string (JsonConvert.SerializeObject(entry.KnowledgeEntities)))
            ("ket", Sql.string (JsonConvert.SerializeObject(entry.KnowledgeEmotionalTraits)))
            ("kbt", Sql.string (JsonConvert.SerializeObject(entry.KnowledgeBehaviouralTraits)))
        ]
        |> Sql.executeNonQueryAsync
        |> Async.map(function | Ok j  -> Ok() | Error exn -> err(exn.Message); Error(exn.Message))

    [<Rpc>]
    let getSymptomJournal(userName:string) : Async<SymptomEntry list option> = 
        pgdb
        |> Sql.query "SELECT * FROM public.physical_symptom_journal WHERE user_name=@u"
        |> Sql.parameters ["u", Sql.string userName]
        |> Sql.executeAsync (fun read -> {
            UserName =  read.string("user_name")
            Date = (read.timestamp "date").ToDateTime() 
            Magnitude = read.intOrNone "magnitude"  
            Location = read.stringOrNone "location"
        }) 
        |> Async.map(function | Ok j  -> Some j | Error exn -> err(exn.Message); None)

    [<Rpc>]
    let getPatients() : Async<Result<Patient list, string>> = 
        pgdb
        |> Sql.query "SELECT * FROM patient"
        |> Sql.executeAsync (fun read ->
        {
            Id =  read.string("Id") |> Models.String
            Sex = Male
            Name = None
            BirthDate = None
            Address = None
        }) 
        |> Async.map(function | Ok r -> Ok r | Error exn -> Error(exn.Message))

    (* expert.ai NLU functions *)
    
    [<Rpc>]
    let getEmotionalTraits(sentence:string): Async<Result<EmotionalTrait list, string>> =
        let from_category(c:Category) = EmotionalTrait(c.Label, (Seq.toList c.Hierarchy), (float) c.Frequency) in
        expertai.AnalyzeEmotionalTraits(sentence)
        |> Async.AwaitTask 
        |> Async.Catch 
        |> Async.map(function 
            | Choice1Of2 r -> r.Categories |> Seq.map from_category |> Seq.toList |> Ok 
            | Choice2Of2 exn -> Error(exn.Message))

    [<Rpc>]
    let getBehavioralTraits(sentence:string): Async<Result<BehavioralTrait list, string>> =
        let from_category(c:Category) = BehavioralTrait(c.Label, (Seq.toList c.Hierarchy), (float) c.Frequency) in
        expertai.AnalyzeBehavioralTraits(sentence)
        |> Async.AwaitTask 
        |> Async.Catch 
        |> Async.map(function 
            | Choice1Of2 r -> r.Categories |> Seq.map from_category |> Seq.toList |> Ok 
            | Choice2Of2 exn -> Error(exn.Message))

    [<Rpc>]
    let getEntities(sentence:string): Async<Result<ExpertAIEntity list, string>> =
        let from_entity(e:Entity) = ExpertAIEntity(e.Type, e.Lemma, (e.Positions |> Seq.map(fun p -> (p.Start, p.End)) |> Seq.toList), e.Relevance) in
        expertai.AnalyzeEntities(sentence)
        |> Async.AwaitTask 
        |> Async.Catch 
        |> Async.map(function 
            | Choice1Of2 r -> r |> Seq.map from_entity |> Seq.toList |> Ok 
            | Choice2Of2 exn -> Error(exn.Message))

    [<Rpc>]
    let getMainLemmas(sentence:string): Async<Result<ExpertAILemma list, string>> =
        let from_lemma(l:MainLemma) = ExpertAILemma(l.Value, (float) l.Score, (l.Positions |> Seq.map(fun p -> (p.Start, p.End)) |> Seq.toList)) in
        expertai.AnalyzeMainLemmas(sentence)
        |> Async.AwaitTask 
        |> Async.Catch 
        |> Async.map(function 
            | Choice1Of2 r -> r |> Seq.map from_lemma |> Seq.toList |> Ok 
            | Choice2Of2 exn -> Error(exn.Message))

    [<Rpc>]
    let getTriples(sentence:string): Async<Result<Knowledge.Triple list list, string>> =
        let rec sub_from_related_item(r:RelatedItem) =
            let rt = if r.Lemma <> "" then r.Lemma  else r.Text
            if (r.Related = null) || r.Related.Count = 0 then 
                Subject rt
            else if r.Related.Count = 1 then
                let rr = r.Related |> Seq.item 0
                let rrt = (if rr.Lemma <> "" then rr.Lemma  else rr.Text)
                Subject.Relation(Knowledge.Relation(rt, rr.Relation, rrt))
            else failwithf "This subject term has more than 1 relation."

        let rec obj_from_related_item(r:RelatedItem) =
            let rt = if r.Lemma <> "" then r.Lemma  else r.Text
            if (r.Related = null) || r.Related.Count = 0 then 
                Knowledge.Object rt
            else if r.Related.Count = 1 then
                let rr = r.Related |> Seq.item 0
                let rrt = (if rr.Lemma <> "" then rr.Lemma  else rr.Text)
                Object.Relation(Knowledge.Relation(rt, rr.Relation, rrt))
            else failwithf "This object term has more than 1 relation."    

        let from_relation(r:Relation) =
            let v = (if r.Verb.Lemma <> "" then r.Verb.Lemma  else r.Verb.Text) |> Verb
            let s = sub_from_related_item (r.Related |> Seq.item 0)
            let rs = r.Related |> Seq.item 0
            if r.Related.Count = 1 then 
                [Triple(SubjectVerbRelation.Relation(s, rs.Relation, v), None)]
            else
                r.Related |> Seq.skip 1 |> Seq.map (fun o -> Triple(SubjectVerbRelation.Relation(s, rs.Relation, v), Some(VerbObjectRelation.Relation(v, o.Relation, (obj_from_related_item o))))) |> Seq.toList
        
        expertai.AnalyzeRelations(sentence)
        |> Async.AwaitTask 
        |> Async.Catch 
        |> Async.map(function 
            | Choice1Of2 r -> r |> Seq.map from_relation |> Seq.toList |> Ok 
            | Choice2Of2 exn -> errex "Exception raised retrieving relations for sentence {0}." exn [sentence]; Error(exn.Message))

    [<Rpc>]
    let getGoogHCNLUEntities(text:string) = 
        GoogleHCApi.AnalyzeEntities2(text) 
        |> Async.AwaitTask
        |> Async.Catch
        |> Async.map(function 
            | Choice1Of2 r -> Ok (r.ToString()) 
            | Choice2Of2 exn -> errex "Exception raised retrieving Google HC NLU entities for sentence {0}." exn [text]; Error(exn.Message))
        
    [<Rpc>]
    let getFHIRPatients(endpoint:string) (q:string array) = 
        let client = FHIR3Client(endpoint)
        client.SearchPatients(q) 
        |> Async.AwaitTask
        |> Async.Catch
        |> Async.map(function 
            | Choice1Of2 r -> Ok (r.ToString()) 
            | Choice2Of2 exn -> errex "Exception raised retrieving patients for query {0}." exn [q]; Error(exn.Message))
        
        
        
