import React, { useState, useEffect } from 'react';
import TypedInput from './TypedInput'
import * as yaxel from './yaxel'
import * as api from './api'

interface FunArgsInputProps {
    params: yaxel.TypedItem[];
    args: any[];
    onSubmit: ((args: any[]) => void);
}

function FunArgsInput(props: FunArgsInputProps) {
    const [args, setArgs] = useState(props.args);
    const onChange = (i: number, x: any) => {
        console.log("FunArgsInput#onChange(" + i + ", " + JSON.stringify(x) + ")");
        args[i] = x;
        setArgs(args);
        props.onSubmit(args);
    }
    useEffect(() => {
        setArgs(props.args);
    }, [props.args])
    return (<form>
        {props.params?.map((p, i) => <TypedInput name={p.name} type={p.type} value={args[i]} onChange={(x) => onChange(i, x)} />)}
    </form>);
}

function Function(props: { module: string, func: yaxel.Fun }) {
    const [args, setArgs] = useState(yaxel.defaultArgsOf(props.func));
    const [result, setResult] = useState(null as any);
    const updateArgs = async (args: any[]) => {
        setArgs(args);
        setResult(await api.invokeFunction(props.module, props.func.name, args));
    }
    useEffect(() => {
        updateArgs(yaxel.defaultArgsOf(props.func));
    }, [props.module, props.func])
    return (
        <div className="Function">
            <hr></hr>
            <h2>'<span className="Function-name">{props.func.name}</span>' function</h2>
            <h3>params</h3>
            {
                <FunArgsInput
                    params={props.func.params}
                    args={args}
                    onSubmit={updateArgs}></FunArgsInput>
            }
            <h3>return</h3>
            {result && result.tag === 'ok' ? <p>{JSON.stringify(result.value)}</p> :
                <div>
                    <h4 style={{ color: "lightpink" }}>ERROR</h4>
                    <p>{result && result.value ? JSON.stringify(result.value) : "(none)"}</p>
                </div>

            }
        </div>
    );
}

export default Function;
