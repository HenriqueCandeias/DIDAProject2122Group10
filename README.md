# Introduction

Group project for Técnico's "Design and Implementation of Distributed Applications" MSc course. Grade: 16.5/20

The goal of this project was to design, implement, and evaluate a simplified version of a **distributed function-as-a-service cloud platform**. This platform can run applications composed of a chain of custom operators that share data via a storage system. Applications are run by sending a script (that specifies the chain of operators to be executed) to a scheduler process. The scheduler starts by assigning a computing node (a worker) to each operator, and then instructs the worker running the first operator to initiate the execution of the chain. The application operators then call each other in a daisy chain sequence. The application data is stored in a storage system maintained by a set of storage nodes.

The design and implementation of the project addresses several challenges raised when building such a distributed system such as: how to access and replicate the application data records, where to execute each operator, and how to maintain data consistency between the system’s storage nodes. The following sections of this document describe how those challenges were faced.

Furthermore, the communication between system components was achieved using **gRPC**.

The course's project statement is present on [DIDA-2122-Project-Statement](https://github.com/HenriqueCandeias/DIDAProject2122Group10/blob/master/DIDA-2122-Project-Statement.pdf).

# Design Decisions

In the following subsections, we present the relevant design decisions that were made to implement the described system.

## Storage Proxy

To provide storage fault tolerance and application consistency for the worker nodes assigned to every application executed by this system, a proxy was implemented in every worker, to provide an interface between the worker node and the storage nodes. This storage proxy finds the right replica to execute the operations.

## Scheduling Algorithm

The algorithm used by the Scheduler node to assign a Worker to every operator that has to be executed by an application is based on a circular buffer. Each index of this buffer corresponds to an individual worker. Thus, the amount of operators (from every application) that is executed by every worker of the system tends to be the same overtime.

## File Location Algorithm

The information in this system is stored in the form of records. Each record is assigned a unique ID when it is created and a new version when it is modified. The algorithm used by every Worker and Storage node to determine the right Storage node to write, update and read a record to/from is based in the concept of Consistent Hashing, this is, it calculates an hash for each record (based on its ID) and all the operations regarding that record are executed in storage nodes with an ID (given by the scheduler in order of creation) greater than itself.

## Consistent Replication among the Storage Nodes

For maintaining proper fault tolerance between the storage nodes we created a pull configuration of Gossip, so that only one storage is receiving and executing operations (writes and updates) while replicating the operations it receives to other replicas. To do this each storage logs all the operations it executes to later replicate to other storage. The operations aren’t replicated to all the other storage nodes but only to a fraction of them, according to the replication factor given in the system configuration. Our Gossip configuration is made on a ”ask” model, this is, each replica will ask the others if they have any operations to replicate to it. To keep track of what operations each replica still needs to replicate we implemented a vector clock that keeps track of how many operations each replica received from another, so when a replica is choosing which operations it needs to send to another one, it compares both clocks and calculates the difference, N, and sends the last N commands executed on itself. This implementation gives a fast performance for reads, since they can be done on any replica that has the given record, and slow performance for writes and updates because they can only be done in the main storage.

## Application Consistency

In this subsection, we explain what has been implemented to ensure that each application maintains a consistent view of the distributed information contained in the various storage nodes throughout the execution of its operators by the corresponding Worker. Every application is assigned a meta record, which is passed along every worker in the chain. When an operator reads, writes, or updates a certain record, it registers the records ID and the new version of that record in the applications meta record. If a following operator reads from that record, it will only read the data associated with that version. Because old versions are eventually discarded by the Storage nodes, if the operator is unable to find the data corresponding to version it is looking for, it signals its own application as inconsistent and prevents the next operators in the chain to be executed.

## Fault tolerance of the Storage Nodes

As mentioned before, the changes propagate to other Storage nodes so that in the event of any storage failures, the Gossip propagation will always replicate records to a determined number of replicas (according to the replication factor). For a faster solution, when a server notices that another server has crashed, it also propagates a ”crash report” to all other Worker and Storage nodes. This way, a delay in which every individual node would have to detect the failure for itself is avoided. When faced with a crash during a write, the storage node will be removed from the working list, meaning that the next working storage will now be the main host of that record.
