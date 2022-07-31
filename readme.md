## Cheers Pivot Tools version 0.1
by Laurie Cheers, 7/31/2022

Install this package anywhere in your Unity assets folder.
Simplest way: Download https://github.com/LaurieCheers/CheersPivotTools/blob/main/~UnityPackage/CheersPivotTools.unitypackage and double click it.

It provides two new EditorTools, available anytime you have a GameObject selected: the Move Pivot Tool and the Rotate Pivot Tool. They're similar to the regular Move Tool and Rotate Tool, except that objects parented to the moved object do not move. (I was getting irritated with this feature not being available, so I created it.)

Multi-select is supported. The rotation handle will be positioned at the first selected object, and other selected objects will rotate around it.

Bonus feature: While either of these tools are selected, if you edit an object's transform position or rotation in the inspector, objects parented to the selected object do not move. (It doesn't matter which of the tools is selected - both tools support this for both fields.)

I hope you find it useful.

Bug reports, fan mail and death threats -> https://github.com/LaurieCheers/CheersPivotTools/issues