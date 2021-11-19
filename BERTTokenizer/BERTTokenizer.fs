namespace BERTTokenizer
open System
open System.IO
open System.Collections.Generic
open System.Text.RegularExpressions

type Features =
    { 
       InputIds         : int list
       InputMask        : int list
       SegmentIds       : int list
       TokenMap         : IDictionary<int,int>
       ///list of tokens 3-tuple: 1) token; 2) optional int indicating the position in the context string and 3) token type 
       Tokens           : (string*int option*int) list
       DebugTokens      : string[]
       ContextTokens    : string list 
    }

module Vocabulary =
    let loadFromFile (path:string) =
        seq {
            use str = new StreamReader(path)
            let mutable line = null
            while (line <- str.ReadLine(); line <> null) do    
            yield line}
        |> Seq.mapi (fun i x -> x,i)
        |> dict    

module Tokenizer =
    let asStr ls = new String(ls |> List.rev |> List.toArray)

    let isControl c = Char.IsControl c
    let isWS c      = Char.IsWhiteSpace c  //seems to cover unicode  Zs, \u2028, \u2029
    let isValid c   = c >= Char.MinValue && c <= Char.MaxValue //not sure we need this
    let isPunc c    = Char.IsPunctuation c

    //state machine
    let rec s_main accW acc = function
        | []                          -> (accW::acc) |> List.filter (List.isEmpty>>not) |> List.map asStr |> List.rev
        | c::rest when isWS c         -> s_ws   (accW::acc) rest
        | c::rest when isControl c    -> s_main accW acc rest                    //skip
        | c::rest when not(isValid c) -> s_main accW acc rest                    //skip
        | c::rest when isPunc c       -> s_main [] ([c]::accW::acc) rest         //punctuation is its own token
        | c::rest                     -> s_main (c::accW) acc rest               //accumulate token
    and s_ws acc = function
        | []                          -> s_main [] acc []
        | c::rest when isWS c         -> s_ws   acc rest                         //skip
        | ls                          -> s_main [] acc ls

    let tokenize toLowerCase (str:string) =
        let str = (if toLowerCase then str.ToLower() else str) |> Seq.toList
        s_main [] [] str

module WordTokenizer =
    [<Literal>]
    let UNK = "[UNK]"

    let MAX_INPUTCHARS_PER_WORD = 200

    let (|Match|_|) (vocab:IDictionary<string,int>) x = 
        let s = Tokenizer.asStr x
        if vocab.TryGetValue(Tokenizer.asStr x) |> fst then 
            Some s
        else 
            None

    let rec findGreedy vocab rem  = function
        | []             -> None
        | '#'::'#'::[]   -> None
        | Match vocab s  -> Some(s,rem)
        | x::rest        -> findGreedy vocab (x::rem) rest

    let rec findPieces vocab addHash allPieces chars =
        if List.length chars = 0 then 
            allPieces |> List.rev
        else
            let revChars = (if addHash then '#'::'#'::chars else chars) |> List.rev
            match findGreedy vocab [] revChars with 
            | Some(wordPiece,rest)  -> findPieces vocab true (wordPiece::allPieces)  rest
            | None                  -> allPieces |> List.rev

    let wordPieces vocab (token:string) = 
        if token.Length > MAX_INPUTCHARS_PER_WORD then
            [UNK]
        else
            match findPieces vocab false [] (Seq.toList token) with 
            | [] -> [UNK] 
            | xs -> xs

type Tokenizer (vocab,toLowerCase,maxSeqLen) =
    member _.tokenize str = 
        Tokenizer.tokenize toLowerCase str
        |> List.collect (WordTokenizer.wordPieces vocab)
        |> List.truncate maxSeqLen

module Featurizer =
    let CLS = "[CLS]"
    let SEP = "[SEP]"

    let padZero len ls = seq {yield! ls; while true do yield 0} |> Seq.truncate len |> Seq.toList

    let seg0 = 0
    let seg1 = 1

    let toFeatures vocab toLowerCase maxQueryLen query context  =

        let queryTokens = 
            query 
            |> Tokenizer.tokenize toLowerCase
            |> List.collect (WordTokenizer.wordPieces vocab)
            |> List.truncate maxQueryLen
            |> List.map (fun x -> x,None,seg0)

        let ctxOrigTokens = Regex.Split(context, @"\s+")  |> Array.toList 
        
        let ctxTokens =
            ctxOrigTokens
            |> List.mapi (fun i t -> t,i)
            |> List.collect (fun (t,i) -> 
                t
                |> Tokenizer.tokenize toLowerCase
                |> List.map (WordTokenizer.wordPieces vocab)
                |> List.collect (fun wts -> wts |> List.map (fun wt -> wt, Some i, seg1)))

        let allTokens =
            seq{
                CLS,  None, seg0
                yield! queryTokens
                SEP, None, seg0
                yield! ctxTokens
            }
            |> Seq.truncate  (maxQueryLen - 1)
            |> fun xs -> Seq.append xs [SEP, None, seg1]
            |> Seq.toList

        let inputIds   = allTokens |> List.map (fun (t,_,_) -> vocab.[t])   |> padZero maxQueryLen 
        let inputMask  = allTokens |> List.map (fun __ -> 1)                |> padZero maxQueryLen
        let segmentIds = allTokens |> List.map (fun (_,_,s) -> s)           |> padZero maxQueryLen

        let tknOrigMap = 
            allTokens 
            |> List.mapi(fun j (_,oi,_) -> j,oi) 
            |> List.choose (fun (j,oi)->oi |> Option.map(fun i -> j,i)) 
            |> dict

        { 
           InputIds         = inputIds
           InputMask        = inputMask
           SegmentIds       = segmentIds
           TokenMap         = tknOrigMap
           Tokens           = allTokens
           ContextTokens    = ctxOrigTokens 
           DebugTokens      = allTokens |> List.map (fun (x,_,_) -> x) |> List.toArray
        }

