/**
 * 
 * @param {string} a
 * @param {number} b
 */
function something(a: string, b: number) {
}

var something2: (a: number, b: string) => {
};

function anotherThing() {
    if (true) {
        return {};
    }
    else {
        var f = 5;
        var g = 6;

        return {
            
            something: ({
            }),

            somethingElse: (a) => {
                return true;
            },

            a: (function () {
            }),

            b: (f * g),

            c: (a,
                b,
                c) => {
            }
        };
    }
}

class Person {
    speak(text: string) : boolean
    {
        if (text != "") {
            console.log(text);
        }
        return false;
    }
    yell(text: string) { alert(text); }
    run() {
    }
    walk(){}
}