import * as yaxel from './yaxel'

export interface Ok<T> {
    tag: "ok";
    value: T;
}
export interface Err {
    tag: "err";
    value: any;
}

export type Result<T> = Ok<T> | Err;

async function postJson<T>(input: RequestInfo, json: string): Promise<Result<T>> {
    const res = await fetch(input, {
        method: 'POST',
        body: json,
        headers: {
            'Content-Type': 'application/json'
        }
    });
    const result = await res.json();
    return result.name === "Ok" ? { tag: 'ok', value: result.value } : { tag: 'err', value: result.value }
}

async function get<T>(func: string): Promise<Result<T>> {
    try {
        const res = await fetch('api/' + func);
        if (res.ok) {
            const json = await res.json();
            return json.name === "Ok" ? { tag: 'ok', value: json.value } : { tag: 'err', value: json.value }
        } else {
            return { tag: 'err', value: { status: res.status } }
        }
    }
    catch (e) {
        console.log(e);
        return { tag: 'err', value: e };
    }
}

async function getOr<T>(func: string, defValue: T): Promise<T> {
    const result = await get<T>(func);
    if (result.tag === 'err') {
        //console.log("ERROR @ getOr(" + func + ", " + JSON.stringify(defValue) + ") > " + JSON.stringify(result.value));
        return defValue;
    } else {
        return result.value;
    }
}

async function fetchDelete(path: string): Promise<Result<null>> {
    const res = await fetch(path, {
        method: 'DELETE',
        body: "",
    });
    if (res.ok) {
        const result = await res.json();
        return result.name === "Ok" ? { tag: 'ok', value: result.value } : { tag: 'err', value: result.value }
    } else {
        return { tag: 'err', value: { status: res.status } };
    }
}

export function fetchModuleList(): Promise<string[]> {
    return getOr<string[]>('modules', []);
}

export function fetchFunctionList(modName: string): Promise<Result<string[]>> {
    return get<string[]>('modules/' + modName + '/functions');
    //return getOr<string[]>('modules/' + modName + '/functions', []);
}

export function fetchBreathCount(): Promise<number> {
    return getOr<number>('modules/breath', -1);
}

export function fetchModuleBreathCount(modName: string): Promise<number> {
    return getOr<number>('modules/' + modName + '/breath', -1);
}

export async function fetchUserCode(modName: string): Promise<string> {
    const result = await get<string>('modules/' + modName + '/usercode');
    return result.tag === 'ok' ? result.value : JSON.stringify(result.value);
}

export function updateUserCode(modName: string, code: string): Promise<Result<any>> {
    return postJson('api/modules/' + modName + '/usercode/update', code);
}

export function invokeFunction(modName: string, funcName: string, args: any[]): Promise<Result<any>> {
    console.log("api.invokeFunction(" + funcName + ", " + JSON.stringify(args) + ")");
    return postJson('api/modules/' + modName + '/functions/' + funcName + '/invoke', JSON.stringify(args));
}

export function fetchFunction(modName: string, funcName: string): Promise<Result<yaxel.Fun>> {
    console.log("api.fetchFunction(" + modName + ", " + funcName + ")")
    return get('modules/' + modName + '/functions/' + funcName);
}

export async function fetchModuleFunctions(modName: string): Promise<Result<yaxel.Fun[]>> {
    const names = await fetchFunctionList(modName);
    if (names.tag === 'err') return names;
    const funcs = new Array<yaxel.Fun>(names.value.length);
    for (let i = 0; i < names.value.length; ++i) {
        const f = await fetchFunction(modName, names.value[i]);
        if (f.tag === 'err') return f;
        funcs[i] = f.value;
    }
    console.log(funcs);
    return { tag: 'ok', value: funcs };
}

export async function addNewModule(modName: string): Promise<Result<null>> {
    return postJson('api/modules/new', modName);
}

export async function deleteModule(modName: string): Promise<Result<null>> {
    return fetchDelete('api/modules/' + modName)
}

export function restoreSampleModules(): Promise<Response> {
    return fetch('api/modules/restore-sample', {
        method: 'POST',
        body: '',
    });
}
