import * as yaxel from './yaxel'

async function fetchText(input: RequestInfo): Promise<string> {
    const res = await fetch(input);
    return res.text();
}

async function fetchBy<T>(input: RequestInfo, map: (text: string) => T): Promise<T> {
    const text = await fetchText(input);
    return map(text);
}

export async function fetchFunctionList(): Promise<string[]> {
    return fetchBy('api/function', JSON.parse)
}

export async function fetchBreathCount(): Promise<number> {
    const text = await fetchText('api/breath/');
    return Number.parseInt(text);
}

export function fetchUserCode(): Promise<string> {
    return fetchText('api/usercode/');
}

export function updateUserCode(code: string) {
    fetch('api/update-usercode', {
        method: 'POST',
        body: code,
        headers: {
            'Content-Type': 'application/json'
        }
    });
}

export async function invokeFunction(funcName: string, args: any[]): Promise<any> {
    console.log("api.invokeFunction(" + funcName + ", " + JSON.stringify(args) + ")");
    const response = await fetch('api/invoke/' + funcName, {
        method: 'POST',
        body: JSON.stringify(args),
        headers: {
            'Content-Type': 'application/json'
        }
    });
    const txt = await response.text();
    try {
        return JSON.parse(txt);
    }
    catch (e) {
        console.log("error @ Function#invoke()");
        console.log(e);
        console.log(txt);
        return txt;
    }
}

export function fetchFunction(funcName: string): Promise<yaxel.Fun> {
    console.log("api.fetchFunction(" + funcName + ")")
    return fetchBy('api/function/' + funcName, JSON.parse);
}
