module.exports = function (callback, e, cookie) {
    var t = new RegExp("(?:^|;\\s*)" + e + "\\=([^;]+)(?:;\\s*|$)").exec(cookie);
    var result = t ? t[1] : void 0
    callback(null, result)
}
    
