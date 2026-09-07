function handler(event) {
  var request = event.request;
  var hostHeader = request.headers.host && request.headers.host.value;
  if (!hostHeader) {
    return request;
  }
  var host = hostHeader.toLowerCase();
  if (host !== "www.lagedra.com") {
    return request;
  }

  var qsParts = [];
  var qs = request.querystring || {};
  Object.keys(qs).forEach(function (key) {
    var item = qs[key];
    if (item.multiValue) {
      item.multiValue.forEach(function (v) {
        qsParts.push(encodeURIComponent(key) + "=" + encodeURIComponent(v.value));
      });
    } else if (item.value !== undefined) {
      qsParts.push(encodeURIComponent(key) + "=" + encodeURIComponent(item.value));
    } else {
      qsParts.push(encodeURIComponent(key));
    }
  });
  var query = qsParts.length ? "?" + qsParts.join("&") : "";

  return {
    statusCode: 301,
    statusDescription: "Moved Permanently",
    headers: {
      location: { value: "https://lagedra.com" + request.uri + query }
    }
  };
}