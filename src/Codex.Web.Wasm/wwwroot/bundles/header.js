var lastQuery = null;
var searchTimerID = -1;
var searchBox = null;

function onBodyLoad() {

    //var anchor = document.location.hash;
    //if (anchor && !document.location.search && document.location.pathname === "/") {
    //    top.location.replace("http://ddindex/" + anchor);
    //    return;
    //}

    // https://github.com/nathancahill/Split.js
    Split(['#leftPane', '#rightPane'], {
        sizes: ['446px', 'calc(100% - 446px)'],
        gutterSize: 20,
        minSize: 1,
        cursor: 'col-resize'
    });

}

function ensureSearchBox() {
    if (typeof searchBox === "object" && searchBox != null) {
        return;
    }

    searchBox = document.getElementById("search-box");

    searchBox.focus();

    searchBox.onkeyup = function () {
        if (event && event.keyCode == 13) {
            onSearchChange();
        }
    };

    searchBox.oninput = function () {
        onSearchChange();
    };
}

const typingThrottleDelay = 600;

function onSearchChange() {
    if (searchBox.value.length > 2) {
        if (searchTimerID == -1) {
            searchTimerID = setTimeout(runSearch, typingThrottleDelay, searchBox.value);
        }
    } else {
        //setLeftPane("<div class='note'>Enter at least 3 characters.</div>");
    }
}

function LoadSearchCore(searchText) {
    if (searchBox.value != searchText || currentState.leftPaneContent != "search") {
        searchBox.value = searchText;
        searchBox.focus();
        runSearch();
    }
}

function runSearch(query) {
    // Reset the timerID so we can kick off the next search timeout
    searchTimerID = -1;

    // if the text has changed since this timer was started, start another timer
    if (query !== searchBox.value) {
        if (searchBox.value.length > 2) {
          searchTimerID = setTimeout(runSearch, typingThrottleDelay, searchBox.value);
        }

        return;
    }

    // Is a search currently running? If so abort it first
    if (typeof lastQuery === "object" && lastQuery !== null) {
        lastQuery.abort();
        lastQuery = null;
    }

    CodexApp.Search(searchBox.value);
}

'use strict';

(function() {

var global = this
  , addEventListener = 'addEventListener'
  , removeEventListener = 'removeEventListener'
  , getBoundingClientRect = 'getBoundingClientRect'
  , isIE8 = global.attachEvent && !global[addEventListener]
  , document = global.document

  , calc = (function () {
        var el
          , prefixes = ["", "-webkit-", "-moz-", "-o-"]

        for (var i = 0; i < prefixes.length; i++) {
            el = document.createElement('div')
            el.style.cssText = "width:" + prefixes[i] + "calc(9px)"

            if (el.style.length) {
                return prefixes[i] + "calc"
            }
        }
    })()
  , elementOrSelector = function (el) {
        if (typeof el === 'string' || el instanceof String) {
            return document.querySelector(el)
        } else {
            return el
        }
    }

  , Split = function (ids, options) {
    var dimension
      , i
      , clientDimension
      , clientAxis
      , position
      , gutterClass
      , paddingA
      , paddingB
      , pairs = []

    // Set defaults

    options = typeof options !== 'undefined' ?  options : {}

    if (typeof options.gutterSize === 'undefined') options.gutterSize = 10
    if (typeof options.minSize === 'undefined') options.minSize = 100
    if (typeof options.snapOffset === 'undefined') options.snapOffset = 30
    if (typeof options.direction === 'undefined') options.direction = 'horizontal'

    if (options.direction == 'horizontal') {
        dimension = 'width'
        clientDimension = 'clientWidth'
        clientAxis = 'clientX'
        position = 'left'
        gutterClass = 'gutter gutter-horizontal'
        paddingA = 'paddingLeft'
        paddingB = 'paddingRight'
        if (!options.cursor) options.cursor = 'ew-resize'
    } else if (options.direction == 'vertical') {
        dimension = 'height'
        clientDimension = 'clientHeight'
        clientAxis = 'clientY'
        position = 'top'
        gutterClass = 'gutter gutter-vertical'
        paddingA = 'paddingTop'
        paddingB = 'paddingBottom'
        if (!options.cursor) options.cursor = 'ns-resize'
    }

    // Event listeners for drag events, bound to a pair object.
    // Calculate the pair's position and size when dragging starts.
    // Prevent selection on start and re-enable it when done.

    var startDragging = function (e) {
            var self = this
              , a = self.a
              , b = self.b

            if (!self.dragging && options.onDragStart) {
                options.onDragStart()
            }

            e.preventDefault()

            self.dragging = true
            self.move = drag.bind(self)
            self.stop = stopDragging.bind(self)

            global[addEventListener]('mouseup', self.stop)
            global[addEventListener]('touchend', self.stop)
            global[addEventListener]('touchcancel', self.stop)

            self.parent[addEventListener]('mousemove', self.move)
            self.parent[addEventListener]('touchmove', self.move)

            a[addEventListener]('selectstart', preventSelection)
            a[addEventListener]('dragstart', preventSelection)
            b[addEventListener]('selectstart', preventSelection)
            b[addEventListener]('dragstart', preventSelection)

            a.style.userSelect = 'none'
            a.style.webkitUserSelect = 'none'
            a.style.MozUserSelect = 'none'
            a.style.pointerEvents = 'none'

            b.style.userSelect = 'none'
            b.style.webkitUserSelect = 'none'
            b.style.MozUserSelect = 'none'
            b.style.pointerEvents = 'none'

            self.gutter.style.cursor = options.cursor
            self.parent.style.cursor = options.cursor

            calculateSizes.call(self)
        }
      , stopDragging = function () {
            var self = this
              , a = self.a
              , b = self.b

            if (self.dragging && options.onDragEnd) {
                options.onDragEnd()
            }

            self.dragging = false

            global[removeEventListener]('mouseup', self.stop)
            global[removeEventListener]('touchend', self.stop)
            global[removeEventListener]('touchcancel', self.stop)

            self.parent[removeEventListener]('mousemove', self.move)
            self.parent[removeEventListener]('touchmove', self.move)

            delete self.stop
            delete self.move

            a[removeEventListener]('selectstart', preventSelection)
            a[removeEventListener]('dragstart', preventSelection)
            b[removeEventListener]('selectstart', preventSelection)
            b[removeEventListener]('dragstart', preventSelection)

            a.style.userSelect = ''
            a.style.webkitUserSelect = ''
            a.style.MozUserSelect = ''
            a.style.pointerEvents = ''

            b.style.userSelect = ''
            b.style.webkitUserSelect = ''
            b.style.MozUserSelect = ''
            b.style.pointerEvents = ''

            self.gutter.style.cursor = ''
            self.parent.style.cursor = ''
        }
      , drag = function (e) {
            var offset

            if (!this.dragging) return

            // Get the relative position of the event from the first side of the
            // pair.

            if ('touches' in e) {
                offset = e.touches[0][clientAxis] - this.start
            } else {
                offset = e[clientAxis] - this.start
            }

            // If within snapOffset of min or max, set offset to min or max

            if (offset <=  this.aMin + options.snapOffset) {
                offset = this.aMin
            } else if (offset >= this.size - this.bMin - options.snapOffset) {
                offset = this.size - this.bMin
            }

            adjust.call(this, offset)

            if (options.onDrag) {
                options.onDrag()
            }
        }
      , calculateSizes = function () {
            // Calculate the pairs size, and percentage of the parent size
            var computedStyle = global.getComputedStyle(this.parent)
              , parentSize = this.parent[clientDimension] - parseFloat(computedStyle[paddingA]) - parseFloat(computedStyle[paddingB])

            this.size = this.a[getBoundingClientRect]()[dimension] + this.b[getBoundingClientRect]()[dimension] + this.aGutterSize + this.bGutterSize
            this.percentage = Math.min(this.size / parentSize * 100, 100)
            this.start = this.a[getBoundingClientRect]()[position]
        }
      , adjust = function (offset) {
            // A size is the same as offset. B size is total size - A size.
            // Both sizes are calculated from the initial parent percentage.

            this.a.style[dimension] = calc + '(' + (offset / this.size * this.percentage) + '% - ' + this.aGutterSize + 'px)'
            this.b.style[dimension] = calc + '(' + (this.percentage - (offset / this.size * this.percentage)) + '% - ' + this.bGutterSize + 'px)'
        }
      , fitMin = function () {
            var self = this
              , a = self.a
              , b = self.b

            if (a[getBoundingClientRect]()[dimension] < self.aMin) {
                a.style[dimension] = (self.aMin - self.aGutterSize) + 'px'
                b.style[dimension] = (self.size - self.aMin - self.aGutterSize) + 'px'
            } else if (b[getBoundingClientRect]()[dimension] < self.bMin) {
                a.style[dimension] = (self.size - self.bMin - self.bGutterSize) + 'px'
                b.style[dimension] = (self.bMin - self.bGutterSize) + 'px'
            }
        }
      , fitMinReverse = function () {
            var self = this
              , a = self.a
              , b = self.b

            if (b[getBoundingClientRect]()[dimension] < self.bMin) {
                a.style[dimension] = (self.size - self.bMin - self.bGutterSize) + 'px'
                b.style[dimension] = (self.bMin - self.bGutterSize) + 'px'
            } else if (a[getBoundingClientRect]()[dimension] < self.aMin) {
                a.style[dimension] = (self.aMin - self.aGutterSize) + 'px'
                b.style[dimension] = (self.size - self.aMin - self.aGutterSize) + 'px'
            }
        }
      , balancePairs = function (pairs) {
            for (var i = 0; i < pairs.length; i++) {
                calculateSizes.call(pairs[i])
                fitMin.call(pairs[i])
            }

            for (i = pairs.length - 1; i >= 0; i--) {
                calculateSizes.call(pairs[i])
                fitMinReverse.call(pairs[i])
            }
        }
      , preventSelection = function () { return false }
      , parent = elementOrSelector(ids[0]).parentNode

    if (!options.sizes) {
        var percent = 100 / ids.length

        options.sizes = []

        for (i = 0; i < ids.length; i++) {
            options.sizes.push(percent)
        }
    }

    if (!Array.isArray(options.minSize)) {
        var minSizes = []

        for (i = 0; i < ids.length; i++) {
            minSizes.push(options.minSize)
        }

        options.minSize = minSizes
    }

    for (i = 0; i < ids.length; i++) {
        var el = elementOrSelector(ids[i])
          , isFirst = (i == 1)
          , isLast = (i == ids.length - 1)
          , size
          , gutterSize = options.gutterSize
          , pair

        if (i > 0) {
            pair = {
                a: elementOrSelector(ids[i - 1]),
                b: el,
                aMin: options.minSize[i - 1],
                bMin: options.minSize[i],
                dragging: false,
                parent: parent,
                isFirst: isFirst,
                isLast: isLast,
                direction: options.direction
            }

            // For first and last pairs, first and last gutter width is half.

            pair.aGutterSize = options.gutterSize
            pair.bGutterSize = options.gutterSize

            if (isFirst) {
                pair.aGutterSize = options.gutterSize / 2
            }

            if (isLast) {
                pair.bGutterSize = options.gutterSize / 2
            }
        }

        // IE9 and above
        if (!isIE8) {
            if (i > 0) {
                var gutter = document.createElement('div')

                gutter.className = gutterClass
                gutter.style[dimension] = options.gutterSize + 'px'

                gutter[addEventListener]('mousedown', startDragging.bind(pair))
                gutter[addEventListener]('touchstart', startDragging.bind(pair))

                parent.insertBefore(gutter, el)

                pair.gutter = gutter
            }

            if (i === 0 || i == ids.length - 1) {
                gutterSize = options.gutterSize / 2
            }

            if (typeof options.sizes[i] === 'string' || options.sizes[i] instanceof String) {
                size = options.sizes[i]
            } else {
                size = calc + '(' + options.sizes[i] + '% - ' + gutterSize + 'px)'
            }

        // IE8 and below
        } else {
            if (typeof options.sizes[i] === 'string' || options.sizes[i] instanceof String) {
                size = options.sizes[i]
            } else {
                size = options.sizes[i] + '%'
            }
        }

        el.style[dimension] = size

        if (i > 0) {
            pairs.push(pair)
        }
    }

    balancePairs(pairs)
}

if (typeof exports !== 'undefined') {
    if (typeof module !== 'undefined' && module.exports) {
        exports = module.exports = Split
    }
    exports.Split = Split
} else {
    global.Split = Split
}

}).call(window);

var defaultWindowTitle = "Index";
var CodexApp;

function onCodexAppStart(codexApp) {
    console.log("Starting codex app JS");
    CodexApp = codexApp;
    ensureSearchBox();
}

function CxNav(link) {
    var url = link.getAttribute("href");
    console.log("Navigating to: " + url);

    CodexApp.Navigate(url);
}

function ReplaceCurrentState() {
    history.replaceState(currentState, currentState.windowTitle, getUrlForState(currentState));
    setPageTitle(currentState.windowTitle);
}

function OnWindowPopState(event) {
    if (event && event.state) {
        DisplayState(event.state);
    }
}

function UpdateState(stateUpdate) {
    var newState = jQuery.extend({}, currentState, stateUpdate);
    NavigateToState(newState);
}

function NavigateToState(state) {
    history.pushState(state, state.windowTitle, getUrlForState(state));
    DisplayState(state);
}

function resetLeftPane() {
    setLeftPane("<div class='note'>Enter a search string. Start with ` for full text search results only.</div>");
}

function setLeftPane(text) {
    if (!text) {
        text = "<div></div>";
    }

    var leftPane = document.getElementById("leftPane");
    leftPane.innerHTML = text;
}

function setRightPane(text) {
    if (!text) {
        text = "<div></div>";
    }

    var rightPane = document.getElementById("rightPane");
    rightPane.innerHTML = text;
}

function setPageTitle(title) {
    if (!title) {
        title = defaultWindowTitle;
    }

    document.title = title;
}

function displayFile(text, symbolId, lineNumber) {
    setRightPane(text);

    var filePath = getFilePath();
    if (filePath && filePath !== currentState.filePath) {
        currentState.filePath = filePath;
        currentState.windowTitle = filePath;
        ReplaceCurrentState();
    }

    GoToSymbolOrLineNumber(symbolId, lineNumber);

    addToolbar();
    trackActiveItemInSolutionExplorer();
}

function getElementByTwoClasses(class1, class2) {
    //return document.querySelector(`.${class1}.${class2}`);
    return $('.' + class1 + '.' + class2);
}

function GoToSymbolOrLineNumber(symbolId, lineNumber) {
    if (symbolId && lineNumber) {
        var matchingElement = getElementByTwoClasses(symbolId, "l" + lineNumber);
        if (matchingElement[0]) {
            matchingElement.scrollTop();
            matchingElement.focus();
            return;
        }
    }

    var blurLine = false;
    if (symbolId) {
        var symbolElement = $('.' + symbolId);;
        if (symbolElement[0]) {
            symbolElement.scrollTop();
            symbolElement.focus();
            return;
        }
        else if (!lineNumber)
        {
            lineNumber = 1;
            symbolId = undefined;
            blurLine = true;
        }
    }

    if (lineNumber) {
        var lineNumberElement = $("#l" + lineNumber);
        if (lineNumberElement[0]) {
            lineNumberElement.scrollTop();
            lineNumberElement.focus();
            if (blurLine)
            {
                lineNumberElement.blur();
            }
        }
    }
}

function ToggleExpandCollapse(headerElement) {
    var collapsible = headerElement.nextElementSibling;
    if (collapsible.style.display == "none") {
        collapsible.style.display = "block";

        headerElement.classList.remove("collapsed");
        headerElement.classList.add("expanded");
    } else {
        collapsible.style.display = "none";

        headerElement.classList.remove("expanded");
        headerElement.classList.add("collapsed");
    }
}

function ToggleFolderIcon(headerElement) {
    var folderIcon = headerElement.firstChild;
    if (!folderIcon) {
        return;
    }

    if (endsWith(folderIcon.src, "202.png")) {
        folderIcon.src = "../../content/icons/201.png";
    } else if (endsWith(folderIcon.src, "201.png")) {
        folderIcon.src = "../../content/icons/202.png";
    }
}

function endsWith(text, suffix) {
    if (!text || !suffix) {
        return false;
    }

    if (suffix.length > text.length) {
        return false;
    }

    var slice = text.slice(text.length - suffix.length, text.length);
    return slice == suffix;
}

function addToolbar() {
    var editorPane = document.getElementById("sourceCode");
    if (!editorPane) {
        return;
    }
    /*
    var documentOutlineButton = document.createElement('img');
    documentOutlineButton.setAttribute('src', '../../content/icons/documentoutline.png');
    documentOutlineButton.title = "Document Outline";
    documentOutlineButton.className = 'documentOutlineButton';
    documentOutlineButton.onclick = showDocumentOutline;
    editorPane.appendChild(documentOutlineButton);

    var projectExplorerButton = document.createElement('img');
    var projectExplorerIcon = '../../content/icons/csharpprojectexplorer.png';

    projectExplorerButton.setAttribute('src', projectExplorerIcon);
    projectExplorerButton.title = "Project Explorer";
    projectExplorerButton.className = 'projectExplorerButton';
    projectExplorerButton.onclick = function () { document.getElementById('projectExplorerLink').click(); };
    editorPane.appendChild(projectExplorerButton);

    var namespaceExplorerButton = document.createElement('img');
    namespaceExplorerButton.setAttribute('src', '../../content/icons/namespaceexplorer.png');
    namespaceExplorerButton.title = "Namespace Explorer";
    namespaceExplorerButton.className = 'namespaceExplorerButton';
    namespaceExplorerButton.onclick = showNamespaceExplorer;
    editorPane.appendChild(namespaceExplorerButton);
    */
}

function trackActiveItemInSolutionExplorer() {
    var projectExplorer = document.getElementById("projectExplorer");
    if (!projectExplorer) {
        return;
    }

    var rootFolderDiv = projectExplorer.firstChild;
    if (rootFolderDiv && (rootFolderDiv.className == "projectCS" || rootFolderDiv.className == "projectVB")) {
        rootFolderDiv = rootFolderDiv.nextElementSibling;
        if (rootFolderDiv) {
            var filePath = getFilePath();
            if (filePath) {
                selectItem(rootFolderDiv, filePath.split("\\"));
            }
        }
    }
}

function selectItem(div, parts) {
    var text = parts[0];
    var found = null;
    for (var i = 0; i < div.children.length; i++) {
        var child = div.children[i];
        if (getInnerText(child) == text) {
            found = child;
            break;
        }
    }

    if (!found) {
        return;
    }

    if (parts.length == 1 && found.tagName == "A") {
        selectFile(found);
    }
    else if (parts.length > 1 && found.tagName == "DIV") {
        found = found.nextElementSibling;
        expandFolderIfNeeded(found);
        selectItem(found, parts.slice(1));
    }
}

function getInnerText(element) {
    if (typeof element.innerText !== "undefined") {
        return element.innerText;
    } else {
        return element.textContent;
    }
}

function expandFolderIfNeeded(folder) {
    if (folder.style.display != "block" && folder && folder.previousSibling && folder.previousSibling.onclick) {
        folder.previousSibling.onclick();
    }
}

function getFilePath() {
    var editorPane = document.getElementById("editorPane");
    if (!editorPane) {
        return;
    }

    var filePath = editorPane.getAttribute("data-filepath");
    return filePath;
}

function selectFile(a) {
    var selected = selectedFile;
    if (selected === a) {
        return;
    }

    if (selected && selected.classList) {
        selected.classList.remove("selectedFilename");
    }

    selectedFile = a;
    if (a) {
        if (a.classList) {
            a.classList.add("selectedFilename");
        }

        scrollIntoViewIfNeeded(a);
    }
}

function scrollIntoViewIfNeeded(element) {
    var topOfPage = window.pageYOffset || document.documentElement.scrollTop || document.body.scrollTop;
    var heightOfPage = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;
    var elY = 0;
    var elH = 0;

    if (document.layers) {
        elY = element.y;
        elH = element.height;
    }
    else {
        for (var p = element; p && p.tagName != 'BODY'; p = p.offsetParent) {
            elY += p.offsetTop;
        }

        elH = element.offsetHeight;
    }

    if ((topOfPage + heightOfPage) < (elY + elH)) {
        element.scrollIntoView(false);
    }
    else if (elY < topOfPage) {
        element.scrollIntoView(true);
    }
}

var currentSelection = null;

// highlight references
function t(sender) {
    var classname = sender.className;

    var elements;
    if (currentSelection) {
        elements = document.getElementsByClassName(currentSelection);
        for (var i = 0; i < elements.length; i++) {
            elements[i].style.background = "transparent";
        }

        if (classname == currentSelection) {
            currentSelection = null;
            return;
        }
    }

    currentSelection = classname;

    elements = document.getElementsByClassName(currentSelection);
    for (var i = 0; i < elements.length; i++) {
        elements[i].style.background = "cyan";
    }
}