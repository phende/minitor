// Style classes per status
var pathClass = {
    "Normal": "text-muted",
    "Warning": "text-warning font-weight-bold",
    "Error": "text-danger font-weight-bold",
    "Unknown": "text-primary font-weight-bold",
    "Dead": "text-dark font-weight-bold"
};
var childClass = {
    "Normal": "btn btn-light btn-sm text-muted child-link",
    "Warning": "btn btn-warning btn-sm child child-link",
    "Error": "btn btn-danger btn-sm text-light child-link",
    "Unknown": "btn btn-primary btn-sm text-light child-link",
    "Dead": "btn btn-dark btn-sm text-light child-link"
};
var monitorClass = {
    "Normal": "bg-light text-muted",
    "Warning": "bg-warning",
    "Error": "bg-danger text-light",
    "Unknown": "bg-primary text-light",
    "Dead": "bg-dark text-light"
};

// Constants
const pathPrefix = "p";
const childPrefix = "c";
const monitorPrefix = "m";
const cellPrefix = "t";

// Usuful nodes
var pathsContainer;
var childrenContainer;
var monitorsTable;
var stateContainer;
var stateLabel;
var stateProgress;
var connectedContainer;

// Global variables
var connectTrials = 0;
var countdown;

//--------------------------------------------------------------------------
function initialize() {
    pathsContainer = document.getElementById("pathsContainer");
    childrenContainer = document.getElementById("childrenContainer");
    monitorsTable = document.getElementById("monitorsTable");
    stateContainer = document.getElementById("stateContainer");
    stateLabel = document.getElementById("stateLabel");
    stateProgress = document.getElementById("stateProgress");
    connectedContainer = document.getElementById("connectedContainer");

    connectSocket();
}

//--------------------------------------------------------------------------
function connectSocket() {
    var uri;

    uri = "ws://";
    if (window.location.protocol == "https:") uri = "wss://";
    uri = uri.concat(window.location.host).concat(window.location.pathname);

    stateLabel.innerText = "Connecting...";
    stateProgress.innerText = "";
    connectTrials = connectTrials + 1;
    if (connectTrials > 6) connectTrials = 6;

    websocket = new WebSocket(uri);
    websocket.onerror = function (evnt) { console.log(evnt); };
    websocket.onmessage = function (evnt) { processEvent(JSON.parse(evnt.data)); };

    websocket.onopen = function (evnt) {
        stateLabel.innerText = "Loading...";
        stateProgress.innerText = "";
        connectTrials = 0;
        resetPage();
    };

    websocket.onclose = function (evnt) {
        stateLabel.innerText = "Disconnected";
        stateProgress.innerText = "";
        connectedContainer.style.display = "none";
        stateContainer.style.display = "block";
        countdown = connectTrials * 5;
        doCountdown();
    };
}

//--------------------------------------------------------------------------
function doCountdown() {
    var str = "";
    for (var i = 0; i < countdown; i++)
        str = str.concat(".");
    stateProgress.innerText = str;

    if (countdown == 0) {
        connectSocket();
        return;
    }

    countdown = countdown - 1;
    window.setTimeout(doCountdown, 1000);
}

//--------------------------------------------------------------------------
// Set page back to initial state with no data
function resetPage() {
    while (pathsContainer.children.length > 0)
        pathsContainer.removeChild(pathsContainer.children[0]);

    while (childrenContainer.children.length > 0)
        childrenContainer.removeChild(childrenContainer.children[0]);

    while (monitorsTable.children.length > 0)
        monitorsTable.removeChild(monitorsTable.children[0]);
}

//--------------------------------------------------------------------------
// Update page data
function processEvent(evnt) {
    stateProgress.innerText = "".concat(
        pathsContainer.children.length + childrenContainer.children.length + monitorsTable.children.length);

    //console.log("".concat(JSON.stringify(evnt)));

    switch (evnt.type) {
        case "ParentChanged":
            setPath(evnt.id, evnt.name, evnt.text, evnt.status);
            break;

        case "BeginInitialize":
            break;

        case "EndInitialize":
            connectedContainer.style.display = "block";
            stateContainer.style.display = "none";
            break;

        case "ChildAdded":
        case "ChildChanged":
            setChild(evnt.id, evnt.name, evnt.text, evnt.status);
            break;

        case "ChildRemoved":
            setChild(evnt.id);
            break;

        case "MonitorAdded":
        case "MonitorChanged":
            setMonitor(evnt.id, evnt.name, evnt.text, evnt.status);
            break;

        case "MonitorRemoved":
            setMonitor(evnt.id);
            break;

        case "StatusChanged":
            setPath(evnt.id, evnt.name, evnt.text, evnt.status);
            break;

        default:
            console.log("Unknown event: ".concat(evnt.type));
            break;
    }
}

//--------------------------------------------------------------------------
// Add or update a breadcrumb link element, should be added in order
function setPath(id, name, path, status) {
    id = pathPrefix.concat(id);
    var link = document.getElementById(id);
    var span;

    if (!name) name = "Home";

    if (link)
        link.className = pathClass[status];
    else {
        span = document.createElement("span");
        if (pathsContainer.children.length > 0)
            span.appendChild(document.createTextNode(" > "));

        link = document.createElement("a");
        link.id = id;
        if (path != null) link.href = "/status/" + encodeURI(path);
        link.className = pathClass[status];
        link.innerText = name;

        span.appendChild(link);
        pathsContainer.appendChild(span);
    }
}

//--------------------------------------------------------------------------
// Add, update or delete a child link element, keep them sorted
function setChild(id, name, path, status) {
    id = childPrefix.concat(id);
    var link = document.getElementById(id);

    if (name) {
        // Update
        if (link)
            link.className = childClass[status];
        // Add
        else {
            link = document.createElement("a");
            link.id = id;
            link.href = "/status/" + encodeURI(path);
            link.className = childClass[status];
            link.innerText = name;
            childrenContainer.appendChild(link);
            sortChildren(childrenContainer, function (e) { return e.innerText; });
        }
    }
    // Remove
    else if (link)
        link.parentElement.removeChild(link);
}


//--------------------------------------------------------------------------
// Add, update or delete a table monitor row element, keep them sorted
function setMonitor(id, name, text, status) {
    id = monitorPrefix.concat(id);
    var row = document.getElementById(id);
    var cell;

    if (!text) text = "";
    if (name) {
        // Update
        if (row) {
            row.className = monitorClass[status];
            cell = document.getElementById(cellPrefix.concat(id));
            cell.innerText = text;
        }
        // Add
        else {
            row = document.createElement("tr");
            row.id = id;
            row.className = monitorClass[status];

            cell = document.createElement("td");
            cell.innerText = name;
            row.appendChild(cell);

            cell = document.createElement("td");
            cell.id = cellPrefix.concat(id);
            cell.innerText = text;
            row.appendChild(cell);

            monitorsTable.appendChild(row);
            sortChildren(monitorsTable, function (e) { return e.children[0].innerText; });
        }
    }
    // Remove
    else if (row)
        row.parentElement.removeChild(row);
}

//--------------------------------------------------------------------------
// Sort children of an element, using a function to retrieve comparison strings
function sortChildren(parent, accessor) {
    var children;
    var i, a, b;
    var swap = true;

    while (swap) {
        swap = false;
        children = parent.children;
        for (i = 0; i < children.length - 1; i++) {
            a = accessor(children[i]);
            b = accessor(children[i + 1]);
            if (a.toLowerCase() > b.toLowerCase()) {
                swap = true;
                break;
            }
        }
        if (swap) children[i].parentNode.insertBefore(children[i + 1], children[i]);
    }
}
