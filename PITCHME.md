#HSLIDE

### Apache Spark
### AWS Lambda Executor
### (SAMBA)

<span style="color:gray">An Apache Spark Package</span>

---

### SAMBA Apache Spark Package

  - Offers seamless integration with the AWS Lambda compute service
  - Within Spark batch and streaming apps on the JVM

---

### SAMBA API

<ol>
<li class="fragment" data-fragment-index="1">New `delegate` operation on RDD[<span style="color:gray">AWSTask</span>]</li>
<li class="fragment" data-fragment-index="2">This operation executes AWS Lambda functions</li>
<li class="fragment" data-fragment-index="3">And generates RDD[<span style="color:gray">AWSResult</span>]</li>
</ol>

<span class="fragment" data-fragment-index="4" style="font-size: 0.8em; color:gray">The SAMBA API is built on top of the <a target="_blank" href="https://github.com/onetapbeyond/aws-gataway-executor">aws-gateway-executor</a> library.</span>

---

### aws-gateway-executor

- A lightweight, fluent Java library
- For calling APIs on the Amazon Web Service API Gateway
- Inside any application running on the JVM
- Defines <span style="color:gray">AWSGateway</span>, <span style="color:gray">AWSTask</span> and <span style="color:gray">AWSResult</span>

+++

### AWSGateway

<span style="color:gray">A handle that represents an API on the AWS API Gateway.</span>

```Java
AWSGateway gateway = AWS.Gateway(echo-api-key)
                        .stage("beta")
                        .region(AWS.Region.OREGON)
                        .build();
```


+++

### AWSTask

<span style="color:gray">An executable object that represents an AWS Gateway call.</span>

```Java
AWSTask aTask = AWS.Task(gateway)
                   .resource("/echo")
                   .get();

```

+++

### AWSResult

<span style="color:gray">An object that represents the result of an AWS Gateway call.</span>

```Java
AWSResult aResult = aTask.execute();
```

---

### SAMBA + Apache Spark Batch Processing

+++

#### Step 1. Build RDD[<span style="color:gray">AWSTask</span>]

```Scala
import io.onetapbeyond.lambda.spark.executor.Gateway._
import io.onetapbeyond.aws.gateway.executor._

val aTaskRDD = dataRDD.map(data => {
  AWS.Task(gateway)
     .resource("/score")
     .input(data.asInput())
     .post()
  })
```

+++

#### Step 2. Delegate RDD[<span style="color:gray">AWSTask</span>]

```Scala
// Perform RDD[AWSTask].delegate operation to execute
// AWS Gateway calls and generate resulting RDD[AWSResult].

val aResultRDD = aTaskRDD.delegate
```

+++

#### Step 3. Process RDD[<span style="color:gray">AWSResult</span>]

```Scala
// Process RDD[AWSResult] data per app requirements. 

aTaskResultRDD.foreach { result => {
        println("TaskDelegation: compute score input=" +
          result.input + " result=" + result.success)
}}
```

+++?gist=494e0fecaf0d6a2aa2acadfb8eb9d6e8

---

#### SAMBA Deployment Architecture

![SAMBA Deployment](https://onetapbeyond.github.io/resource/img/samba/new-samba-deploy.jpg)

---

#### Some Related Links

- [GitHub: SAMBA Package](https://github.com/onetapbeyond/lambda-spark-executor)
- [GitHub: SAMBA Examples](https://github.com/onetapbeyond/lambda-spark-executor#samba-examples)
- [GitHub: aws-gateway-executor](https://github.com/onetapbeyond/aws-gateway-executor)
- [GitHub: Apache Spark](https://github.com/apache/spark)
- [Apache Spark Packages](https://spark-packages.org/package/onetapbeyond/lambda-spark-executor)
