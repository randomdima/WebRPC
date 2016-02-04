window.$ = {
    call: function (member, args) {
        var xmlhttp = new XMLHttpRequest();
        xmlhttp.onreadystatechange = $.onResponse;

        var request = args[0];
        //named arguments request
        if (args.length == 1 && typeof request == 'object') {
            xmlhttp.CallBack = request.CallBack;
            delete request.CallBack;
        }
            //array arguments request
        else {
            request = null;
            var argLen = args.length;
            if (typeof args[argLen - 1] == 'function')
                xmlhttp.CallBack = args[--argLen];
            var ArrReq = Array.prototype.slice.call(args, 0, argLen);
            var q = Math.min(ArrReq.length,member.ParamKeys.length);
            if(q)request={};
            while (q--) request[member.ParamKeys[q]] = ArrReq[q];
        }

        var StrReq;
        if (request && (member.PostParams || (StrReq = JSON.stringify(request)).length > 950)) {
            var formData = new FormData();
            if (member.PostParams)
                for (var q = 0; q < member.PostParams.length; q++) {
                    var name = member.PostParams[q];
                    var val=request[name];
                    delete request[name];
                    if(Array.isArray(val))
                        for(var w=0;w<val.length;w++)
                            formData.append(name,val[w]);
                    else
                        if (val != null)
                            formData.append(name, val);
                }

            if (!StrReq) StrReq = JSON.stringify(request);
            if (StrReq) formData.append('', StrReq);

            xmlhttp.open("POST", $.root + member.url, true);
            xmlhttp.send(formData);
        }
        else {
            if (StrReq) StrReq = "?" + encodeURIComponent(StrReq);
            else StrReq = '';
            xmlhttp.open("GET", $.root + member.url + StrReq, true);
            xmlhttp.send(null);
        }
    },
    onError: function (E) { alert(E); },
    onResponse: function () {
        if (this.readyState != 4) return;
        if (this.status == 200) {
            if (this.CallBack)
                try {
                    this.CallBack(eval('(' + this.responseText.replace(/\\n/g, '\\\\n') + ')'));
                }
                catch (E) {
                    $.onError(E.toString(), this);
                }
        }
        else if (this.status == 0) $.onError('Server not responding', this);
        else $.onError(this.responseText, this);
    },
    init: function () {
        var me = this;
        for (var q in me.info.Members) {
            if (me.info.Members[q]['$type'] != 'RPCMemberInfo') continue;
            var parts = q.split('/');
            var fn = function () { me.call(arguments.callee, arguments); };
            fn.url = q;
            //flag for Binders
            fn.callback = true;

            for (var w in me.info.Members[q])
                fn[w] = me.info.Members[q][w];

            if (fn.ReturnType.indexOf('[]')>0) {
                fn.IsArray = true;
                fn.ReturnType = fn.ReturnType.substring(0, fn.ReturnType.length - 2);
            }
            fn.ReturnType = me.info.CustomTypes[fn.ReturnType];
            fn.ParamKeys = Object.keys(fn.Parameters);

            for (var q in fn.Parameters)
                if (fn.Parameters[q] == 'HttpPostedFile' || fn.Parameters[q] == 'HttpFileCollection'){
                    if(!fn.PostParams)fn.PostParams=[];
                    fn.PostParams.push(q);
                }

            var type = parts[0];
            var name = parts[1];

            if (!me[type]) me[type] = { IsClass: true };
            me[type][name] = fn;
        }
    }
};
