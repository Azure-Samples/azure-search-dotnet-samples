import React, { useEffect, useState } from 'react';
import axios from 'axios';
import apiBaseUrl from "../../config";

export default function Status() {
  
  const [ results, setResults ] = useState([]);
  const [ isLoading, setIsLoading ] = useState(true);
  
  useEffect(() => {
    setIsLoading(true);

    axios.get( `${apiBaseUrl || ""}/api/status`)
      .then(response => {
            console.log(JSON.stringify(response.data))
            setResults(response.data.results);
            setIsLoading(false);
        } )
        .catch(error => {
            console.log(error);
            setIsLoading(false);
        });
    
  }, []);

  var body;
  if (!isLoading) {
    body = (
      <div className="col-md-9">
        {JSON.stringify(results)}
      </div>
    )
  }

  return (
    <main className="main main--search container-fluid">
      <div className="row">
        {body}
      </div>
    </main>
  );
}
