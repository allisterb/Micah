namespace Micah.Web

open WebSharper
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.JQuery

module Resources =
    open WebSharper.Core.Resources
    
    type CSS() =
        inherit BaseResource("https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/css", "bootstrap.min.css")
    type PopperJS() =
        inherit BaseResource("https://cdn.jsdelivr.net/npm/popper.js@1.16.1/dist/umd", "popper.min.js")
    type JS() =
        inherit BaseResource("https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js", "bootstrap.min.js")

[<Require(typeof<Resources.CSS>);Require(typeof<JQuery.Resources.JQuery>);Require(typeof<Resources.PopperJS>);Require(typeof<Resources.JS>)>]
[<JavaScript>]
module Bs =
    
    let btnPrimary label onclick = button [reid "btn"; cls "btn btn-primary"; on.click onclick] [text label]
    let btnSecondary label onclick = button [reid "btn"; cls "btn btn-secondary"; on.click onclick] [text label]
    let btnSuccess label onclick = button [reid "btn"; cls "btn btn-success"; on.click onclick] [text label]
    let btnDanger label onclick = button [reid "btn"; cls "btn btn-danger"; on.click onclick] [text label]
    let btnWarning label onclick = button [reid "btn"; cls "btn btn-warning"; on.click onclick] [text label]
    let btnInfo label onclick = button [reid "btn"; cls "btn btn-info"; on.click onclick] [text label]
    let btnLight label onclick = button [reid "btn"; cls "btn btn-light"; on.click onclick] [text label]
    let btnDark label onclick = button [reid "btn"; cls "btn btn-dark"; on.click onclick] [text label]

    let input lbl extras (target, labelExtras, targetExtras) =
        div (cls "form-group" :: extras) [
            label labelExtras [text lbl]
            Doc.Input [cls "form-control"; targetExtras] target
        ]

    let inputPassword lbl extras (target, labelExtras, targetExtras) =
        div (cls "form-group" :: extras) [
            label labelExtras [text lbl]
            Doc.PasswordBox (cls "form-control" :: targetExtras) target
        ]

    let textArea lbl extras (target, labelExtras, targetExtras) =
        div (cls "form-group" :: extras) [
            label labelExtras [text lbl]
            Doc.InputArea (cls "form-control" :: targetExtras) target
        ]

    let checkbox lbl extras (target, labelExtras, targetExtras) =
        div (cls "checkbox" :: extras) [
            label labelExtras [
                Doc.CheckBox targetExtras target
                text lbl
            ]
        ]

    let Radio lbl extras (target, labelExtras, targetExtras) =
        div (cls "radio" :: extras) [
            label labelExtras [
                Doc.Radio targetExtras true target
                text lbl
            ]
        ]
